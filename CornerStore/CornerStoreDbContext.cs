using Microsoft.EntityFrameworkCore;
using CornerStore.Models;
public class CornerStoreDbContext : DbContext
{

    public DbSet<Cashier> Cashiers { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<Order> Orders { get; set; }
    public DbSet<OrderProduct> OrderProducts { get; set; }
    public DbSet<Product> Products { get; set; }

    public CornerStoreDbContext(DbContextOptions<CornerStoreDbContext> context) : base(context)
    {

    }

    //allows us to configure the schema when migrating as well as seed data
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Seed Cashiers
        modelBuilder.Entity<Cashier>().HasData(new Cashier[]
        {
        new Cashier { Id = 1, FirstName = "Ernie", LastName = "Fairchild" },
        new Cashier { Id = 2, FirstName = "Lana", LastName = "Lopez" }
        });

        // Seed Categories
        modelBuilder.Entity<Category>().HasData(new Category[]
        {
        new Category { Id = 1, CategoryName = "Beverages" },
        new Category { Id = 2, CategoryName = "Snacks" },
        new Category { Id = 3, CategoryName = "Household" }
        });

        // Seed Products
        modelBuilder.Entity<Product>().HasData(new Product[]
        {
        new Product { Id = 1, ProductName = "Cola", Price = 1.25M, Brand = "FizzCo", CategoryId = 1 },
        new Product { Id = 2, ProductName = "Chips", Price = 1.50M, Brand = "Crunchies", CategoryId = 2 },
        new Product { Id = 3, ProductName = "Paper Towels", Price = 2.75M, Brand = "CleanUp", CategoryId = 3 },
        new Product { Id = 4, ProductName = "Water Bottle", Price = 1.00M, Brand = "AquaPure", CategoryId = 1 }
        });

        // Seed Orders
        modelBuilder.Entity<Order>().HasData(new Order[]
        {
        new Order { Id = 1, CashierId = 1, PaidOnDate = new DateTime(2025, 5, 20) },
        new Order { Id = 2, CashierId = 2, PaidOnDate = new DateTime(2025, 5, 21) }
        });

        // Seed OrderProducts
        modelBuilder.Entity<OrderProduct>().HasData(new OrderProduct[]
        {
        new OrderProduct { Id = 1, OrderId = 1, ProductId = 1, Quantity = 2 }, // 2x Cola
        new OrderProduct { Id = 2, OrderId = 1, ProductId = 2, Quantity = 1 }, // 1x Chips
        new OrderProduct { Id = 3, OrderId = 2, ProductId = 3, Quantity = 1 }, // 1x Paper Towels
        new OrderProduct { Id = 4, OrderId = 2, ProductId = 4, Quantity = 3 }  // 3x Water Bottle
        });
    }
}