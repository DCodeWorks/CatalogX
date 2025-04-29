using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CatalogX.Infrastructure
{
    public class ReplicaCatalogXDbContext : CatalogXDbContext
    {
        public ReplicaCatalogXDbContext(DbContextOptions<CatalogXDbContext> options) : base(options)
        {
        }
    }
}
