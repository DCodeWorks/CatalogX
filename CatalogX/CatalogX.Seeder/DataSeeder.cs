using CatalogX.Domain;
using CatalogX.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CatalogX.Seeder
{
    public static class DataSeeder
    {
        public static async Task SeedProducts(CatalogXDbContext dbContext, int totalRecords, int batchSize = 10000)
        {
            // Optional: Abort if the table already has data.
            if (dbContext.Products.Any())
            {
                Console.WriteLine("Products table already contains data. Seeding aborted.");
                return;
            }

            var random = new Random();
            var products = new List<Product>();

            for (int i = 0; i < totalRecords; i++)
            {
                var product = new Product
                {
                    Name = $"Product {i + 1}",
                    Description = $"Description for product {i + 1}",
                    Price = Convert.ToDecimal(random.NextDouble() * 100), // Price between 0 and 100
                    Category = $"Category {(i % 10) + 1}" // Distribute products across 10 categories
                };

                products.Add(product);

                if (products.Count >= batchSize)
                {
                    await dbContext.Products.AddRangeAsync(products);
                    await dbContext.SaveChangesAsync();
                    Console.WriteLine($"{i + 1} products seeded...");
                    products.Clear();
                }
            }

            if (products.Any())
            {
                await dbContext.Products.AddRangeAsync(products);
                await dbContext.SaveChangesAsync();
                Console.WriteLine($"{totalRecords} products seeded.");
            }
        }
    }
}
