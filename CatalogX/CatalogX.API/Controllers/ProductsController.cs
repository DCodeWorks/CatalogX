using CatalogX.Domain;
using CatalogX.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using System.Text.Json;

namespace CatalogX.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductsController : ControllerBase
    {
        private readonly CatalogXDbContext _writeDbContext;
        //private readonly ReplicaCatalogXDbContext _readDbContext;
        private readonly IConnectionMultiplexer _redis;
        private readonly ILogger<ProductsController> _logger;
        public ProductsController(
            CatalogXDbContext catalogXDbContext,
            ReplicaCatalogXDbContext replicaCatalogXDbContext,
            IConnectionMultiplexer redis,
            ILogger<ProductsController> logger)
        {
            _writeDbContext = catalogXDbContext;
            //_readDbContext = replicaCatalogXDbContext;
            _redis = redis;
            _logger = logger;
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetProduct(int id)
        {
            var product = await _writeDbContext.Products.SingleOrDefaultAsync(x => x.Id == id);

            if (product == null)
                return NotFound();

            return Ok(product);
        }

        [HttpPost]
        public async Task<IActionResult> createProduct([FromBody] Product product)
        {
            _writeDbContext.Products.Add(product);
            await _writeDbContext.SaveChangesAsync();

            var server = _redis.GetServer(_redis.GetEndPoints()[0]);
            foreach (var key in server.Keys(pattern: "products:adv:*"))
            {
                await _redis.GetDatabase().KeyDeleteAsync(key);
            }

            return CreatedAtAction(nameof(GetProduct), new { id = product.Id }, product);

        }

        [HttpGet]
        public async Task<IActionResult> GetProducts([FromQuery] ProductQueryParameters queryParams)
        {
            var db = _redis.GetDatabase();

            var cacheKey = $"products:adv:pg={queryParams.PageNumber}:sz={queryParams.PageSize}"
                     + (string.IsNullOrEmpty(queryParams.Category) ? "" : $":cat={queryParams.Category}")
                     + (string.IsNullOrEmpty(queryParams.Search) ? "" : $":q={queryParams.Search}");

            var cached = await db.StringGetAsync(cacheKey);
            if (cached.HasValue)
            {
                _logger.LogInformation("Cache hit for {Key}", cacheKey);
                return Content(cached, "application/json");
            }

            _logger.LogInformation("Received request for products with search parameter: {Search}", queryParams.Search);

            IQueryable<Product> query = _writeDbContext.Products.AsNoTracking();

            if (queryParams.MinPrice.HasValue)
                query = query.Where(p => p.Price >= queryParams.MinPrice.Value);

            if (queryParams.MaxPrice.HasValue)
                query = query.Where(p => p.Price <= queryParams.MaxPrice.Value);

            if (!string.IsNullOrEmpty(queryParams.Category))
                query = query.Where(p => p.Category == queryParams.Category);

            /*if (!string.IsNullOrEmpty(queryParams.Search))
            {
                query = query.Where(p => p.Name.Contains(queryParams.Search) ||
                p.Description.Contains(queryParams.Search));
            }*/

            // Use full-text search for the Search parameter
            if (!string.IsNullOrEmpty(queryParams.Search))
            {
                var quotedSearch = $"\"{queryParams.Search}\"";
                // This leverages SQL Server's CONTAINS function behind the scenes
                query = query.Where(p => EF.Functions.Contains(p.Name, quotedSearch)
                                         || EF.Functions.Contains(p.Description, quotedSearch));
            }

            var totalCount = await query.CountAsync();

            var products = await query
                .OrderBy(p => p.Id)
                .Skip((queryParams.PageNumber - 1) * queryParams.PageSize)
                .Take(queryParams.PageSize)
                .ToListAsync();

            _logger.LogInformation("Returning {Count} products for page {PageNumber}", products.Count, queryParams.PageNumber);

            var response = new
            {
                totalCount = totalCount,
                PageNumber = queryParams.PageNumber,
                PageSize = queryParams.PageSize,
                Data = products
            };

            var json = JsonSerializer.Serialize(response);
            await db.StringSetAsync(cacheKey, json, TimeSpan.FromSeconds(30));

            return Ok(response);

        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            var product = await _writeDbContext.Products.FindAsync(id);

            if (product == null)
                return NotFound();

            _writeDbContext.Products.Remove(product);
            await _writeDbContext.SaveChangesAsync();

            var server = _redis.GetServer(_redis.GetEndPoints()[0]);
            foreach (var key in server.Keys(pattern: "products:adv:*"))
            {
                await _redis.GetDatabase().KeyDeleteAsync(key);
            }

            return Ok(product);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateProduct(int id,[FromBody] Product product)
        {
            var productFromDB = await _writeDbContext.Products.FindAsync(id);

            if (product == null)
                return NotFound();

            productFromDB.Name = product.Name;
            productFromDB.Description = product.Description;    
            productFromDB.Category = product.Category;
            productFromDB.Price = product.Price;

            await _writeDbContext.SaveChangesAsync();

            var server = _redis.GetServer(_redis.GetEndPoints()[0]);
            foreach (var key in server.Keys(pattern: "products:adv:*"))
            {
                await _redis.GetDatabase().KeyDeleteAsync(key);
            }

            return Ok(product);
        }
    }
}
