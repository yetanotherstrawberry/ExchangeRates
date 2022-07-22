using ExchangeRatesAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace ExchangeRatesAPI
{
    public class ApplicationDbContext : DbContext
    {
        public DbSet<Exchange> Exchanges { get; set; }
        public DbSet<Request> Requests { get; set; }
        public DbSet<ApiKey> Tokens { get; set; }

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Exchange>().HasKey(x => x.Date);

            modelBuilder.Entity<Currency>().Property<int>("Id");
            modelBuilder.Entity<Currency>().HasKey("Id");

            modelBuilder.Entity<ExchangeRate>().Property<int>("Id");
            modelBuilder.Entity<ExchangeRate>().HasKey("Id");

            modelBuilder.Entity<ApiKey>().HasKey(x => x.Key);
            modelBuilder.Entity<ApiKey>().HasIndex(x => x.Created);

            modelBuilder.Entity<Request>().Property<int>("Id");
            modelBuilder.Entity<Request>().HasKey("Id");
            modelBuilder.Entity<Request>().HasIndex(x => x.RequestDate);
            modelBuilder.Entity<Request>()
                .HasOne(x => x.apiKey).WithMany()
                .IsRequired().OnDelete(DeleteBehavior.Restrict); // Do not allow to delete tokens if they were used.
        }
    }
}
