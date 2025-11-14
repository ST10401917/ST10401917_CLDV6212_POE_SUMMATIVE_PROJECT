using System;
using Microsoft.EntityFrameworkCore;
using CLDV6212_POE_PART_1.Models;

namespace CLDV6212_POE_PART_1.Data
{
    public class CLDV6212_POE_PART_1Context : DbContext
    {
        public CLDV6212_POE_PART_1Context(DbContextOptions<CLDV6212_POE_PART_1Context> options)
            : base(options)
        {
        }

        public DbSet<Customer> Customers { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<User> Users { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ✅ Ignore Azure Table Storage ETag so EF does NOT try to map it to SQL
            modelBuilder.Entity<Customer>().Ignore(c => c.ETag);
            modelBuilder.Entity<Product>().Ignore(p => p.ETag);
            modelBuilder.Entity<Order>().Ignore(o => o.ETag);
        }
    }
}
