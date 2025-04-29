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
        private readonly ReplicaCatalogXDbContext _readDbContext;
        private readonly IConnectionMultiplexer _redis;
        private readonly ILogger<ProductsController> _logger;
        public ProductsController(
            CatalogXDbContext catalogXDbContext,
            ReplicaCatalogXDbContext replicaCatalogXDbContext,
            IConnectionMultiplexer redis,
            ILogger<ProductsController> logger)
        {
            _writeDbContext = catalogXDbContext;
            _readDbContext = replicaCatalogXDbContext;
            _redis = redis;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetProducts()
        {
            // Simple caching key for demonstration purposes.
            var cacheKey = "products_list";

            var db = _redis.GetDatabase();
            var cachedProducts = await db.StringGetAsync(cacheKey);

            if (cachedProducts.HasValue)
            {
                var products = JsonSerializer.Deserialize<List<Product>>(cachedProducts);
                return Ok(products);
            }

            var dbProducts = await _writeDbContext.Products.AsNoTracking().ToListAsync();

            var serializedProducts = JsonSerializer.Serialize(dbProducts);
            await db.StringSetAsync(cacheKey, serializedProducts, TimeSpan.FromMinutes(5));

            return Ok(dbProducts);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetProduct(int id)
        {
            var product = await _readDbContext.Products.SingleOrDefaultAsync(x => x.Id == id);

            if (product == null)
                return NotFound();

            return Ok(product);
        }

        [HttpPost]
        public async Task<IActionResult> createProduct([FromBody] Product product)
        {
            _writeDbContext.Products.Add(product);
            await _writeDbContext.SaveChangesAsync();

            var db = _redis.GetDatabase();
            await db.KeyDeleteAsync("products_list");

            return CreatedAtAction(nameof(GetProduct), new { id = product.Id }, product);

        }

        [HttpGet("advanced")]
        public async Task<IActionResult> GetProductsAdvanced([FromQuery] ProductQueryParameters queryParams)
        {
            _logger.LogInformation("Received request for products with search parameter: {Search}", queryParams.Search);

            IQueryable<Product> query = _readDbContext.Products.AsNoTracking();

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

            return Ok(response);

        }

        [HttpGet("advancedPoor")]
        public async Task<IActionResult> GetProductsAdvancedPoor([FromQuery] ProductQueryParameters queryParams)
        {
            var query = _writeDbContext.Products
            .Where(p =>
                (!queryParams.MinPrice.HasValue || p.Price >= queryParams.MinPrice.Value) &&
                (!queryParams.MaxPrice.HasValue || p.Price <= queryParams.MaxPrice.Value) &&
                (string.IsNullOrEmpty(queryParams.Category) || p.Category == queryParams.Category) &&
                (string.IsNullOrEmpty(queryParams.Search) ||
                    p.Name.Contains(queryParams.Search) ||
                    p.Description.Contains(queryParams.Search)));

            var totalCount = await query.CountAsync();

            var products = await query
                .OrderBy(p => p.Id)
                .Skip((queryParams.PageNumber - 1) * queryParams.PageSize)
                .Take(queryParams.PageSize)
                .ToListAsync();

            var response = new
            {
                totalCount = totalCount,
                PageNumber = queryParams.PageNumber,
                PageSize = queryParams.PageSize,
                Data = products
            };

            return Ok(response);

        }
    }
}
