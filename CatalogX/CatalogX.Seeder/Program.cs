
// Build configuration from appsettings.json
using CatalogX.Infrastructure;
using CatalogX.Seeder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

var configuration = new ConfigurationBuilder()
.SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .Build();

// Setup DbContext options using the SQL connection string
var optionsBuilder = new DbContextOptionsBuilder<CatalogXDbContext>();
var connectionString = configuration.GetConnectionString("DefaultConnection");
optionsBuilder.UseSqlServer(connectionString);

// Create an instance of CatalogXDbContext
using (var dbContext = new CatalogXDbContext(optionsBuilder.Options))
{
    Console.WriteLine("Starting product seeding...");

    // Set the desired number of records to seed (e.g., 1,000,000)
    int totalRecords = 10000000;
    Console.WriteLine($"Seeding {totalRecords} products...");

    await DataSeeder.SeedProducts(dbContext, totalRecords, batchSize: 10000);

    Console.WriteLine("Seeding complete.");
}