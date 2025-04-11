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
        private readonly CatalogXDbContext _dbContext;
        private readonly IConnectionMultiplexer _redis;
        public ProductsController(
            CatalogXDbContext catalogXDbContext,
            IConnectionMultiplexer redis)
        {
            _dbContext = catalogXDbContext;
            _redis = redis;
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

            var dbProducts = await _dbContext.Products.AsNoTracking().ToListAsync();

            var serializedProducts = JsonSerializer.Serialize(dbProducts);
            await db.StringSetAsync(cacheKey, serializedProducts, TimeSpan.FromMinutes(5));

            return Ok(dbProducts);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetProduct(int id)
        {
            var product = await _dbContext.Products.SingleOrDefaultAsync(x => x.Id == id);

            if (product == null)
                return NotFound();

            return Ok(product);
        }

        [HttpPost]
        public async Task<IActionResult> createProduct([FromBody] Product product)
        {
            _dbContext.Products.Add(product);
            await _dbContext.SaveChangesAsync();

            var db = _redis.GetDatabase();
            await db.KeyDeleteAsync("products_list");

            return CreatedAtAction(nameof(GetProduct), new { id = product.Id }, product);

        }
    }
}
