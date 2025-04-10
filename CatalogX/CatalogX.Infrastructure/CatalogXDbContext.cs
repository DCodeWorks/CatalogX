using CatalogX.Domain;
using Microsoft.EntityFrameworkCore;

namespace CatalogX.Infrastructure
{
    public class CatalogXDbContext : DbContext
    {
        public CatalogXDbContext(DbContextOptions<CatalogXDbContext> options) : base(options) { }
        
        public DbSet<Product> Products { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Product>()
                .HasIndex(p => p.Category);

            modelBuilder.Entity<Product>()
                .HasIndex(p => p.Name);

            base.OnModelCreating(modelBuilder);
        }
    }
}
