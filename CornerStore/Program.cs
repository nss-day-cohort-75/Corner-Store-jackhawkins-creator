using CornerStore.Models;
using CornerStore.Models.DTOs;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http.Json;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
// allows passing datetimes without time zone data 
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

// allows our api endpoints to access the database through Entity Framework Core and provides dummy value for testing
builder.Services.AddNpgsql<CornerStoreDbContext>(builder.Configuration["CornerStoreDbConnectionString"] ?? "testing");

// Set the JSON serializer options
builder.Services.Configure<JsonOptions>(options =>
{
    options.SerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

//endpoints go here
//A. CASHIER ENDPOINTS
//0. GET All Cashiers (not necessary but used for testing)
app.MapGet("/api/cashiers", (CornerStoreDbContext db) =>
{
    return db.Cashiers
    .Select(c => new CashierDTO
    {
        Id = c.Id,
        FirstName = c.FirstName,
        LastName = c.LastName
    }).ToList();
});

//1. POST Cashier
/* TEST
{
  "firstName": "New",
  "lastName": "Bloke"
}
*/
app.MapPost("/api/cashiers", (CornerStoreDbContext db, Cashier cashier) =>
{
    db.Cashiers.Add(cashier);
    db.SaveChanges();
    return Results.Created($"/api/cashiers/{cashier.Id}", new CashierDTO
    {
        Id = cashier.Id,
        FirstName = cashier.FirstName,
        LastName = cashier.LastName
    });
});

//2. GET 1 Cashier (w/ Orders + Products)
app.MapGet("/api/cashiers/{id}", (CornerStoreDbContext db, int id) =>
{
    return db.Cashiers
        .Include(c => c.Orders) //list of orders made by this cashier
            .ThenInclude(o => o.OrderProducts) //include orderproduct join for each order
                .ThenInclude(op => op.Product) //product for each orderproduct
        .Where(c => c.Id == id) //filter cashier by id
        .Select(c => new CashierDTO
        {
            Id = c.Id,
            FirstName = c.FirstName,
            LastName = c.LastName,
            Orders = c.Orders.Select(o => new OrderDTO
            {
                Id = o.Id,
                CashierId = o.CashierId,
                PaidOnDate = o.PaidOnDate,
                OrderProducts = o.OrderProducts.Select(op => new OrderProductDTO
                {
                    Id = op.Id,
                    ProductId = op.ProductId,
                    OrderId = op.OrderId,
                    Quantity = op.Quantity,
                    Product = new ProductDTO
                    {
                        Id = op.Product.Id,
                        ProductName = op.Product.ProductName,
                        Price = op.Product.Price,
                        Brand = op.Product.Brand,
                        CategoryId = op.Product.CategoryId
                    }
                }).ToList()
            }).ToList()
        })
        .SingleOrDefault();
});

//B. PRODUCT ENDPOINTS
//1. GET All Products (w/ Categories + Opt. Search Query Prod. OR Cat. Name)
//TEST w/ http://localhost:5000/api/products?search=snacks
app.MapGet("/api/products", (CornerStoreDbContext db, string? search) =>
{
    //queryable collection of products w/ their categories
    IQueryable<Product> query = db.Products.Include(p => p.Category);

    if (!string.IsNullOrWhiteSpace(search)) //if no whitespace, then...
    {
        string lowerSearch = search.ToLower();

        //filter so it includes either (a) product name OR (b) category name
        query = query.Where(p =>
            p.ProductName.ToLower().Contains(lowerSearch) ||
            p.Category.CategoryName.ToLower().Contains(lowerSearch));
    }

    List<ProductDTO> products = query
        .Select(p => new ProductDTO
        {
            Id = p.Id,
            ProductName = p.ProductName,
            Price = p.Price,
            Brand = p.Brand,
            CategoryId = p.CategoryId,
            Category = new CategoryDTO
            {
                Id = p.Category.Id,
                CategoryName = p.Category.CategoryName
            }
        }).ToList();

    return Results.Ok(products);
});

//2. POST Product
/*TEST w/
{
  "productName": "Organic Granola Bar",
  "price": 1.99,
  "brand": "HealthySnacks Co.",
  "categoryId": 2
}
*/
app.MapPost("/api/products", (CornerStoreDbContext db, Product product) =>
{
    db.Products.Add(product);
    db.SaveChanges();
    return Results.Created($"/api/products/{product.Id}", new ProductDTO
    {
        Id = product.Id,
        ProductName = product.ProductName,
        Price = product.Price,
        Brand = product.Brand,
        CategoryId = product.CategoryId
    });
});

//3. PUT Product
/*TEST w/
{
  "productName": "Updated Granola Bar",
  "price": 2.49,
  "brand": "HealthySnacks Co. Plus",
  "categoryId": 2
}
*/
app.MapPut("/api/products/{id}", (CornerStoreDbContext db, int id, ProductDTO productDTO) =>
{
    Product productToUpdate = db.Products.SingleOrDefault(product => product.Id == id);
    if (productToUpdate == null)
    {
        return Results.NotFound();
    }
    productToUpdate.ProductName = productDTO.ProductName;
    productToUpdate.Price = productDTO.Price;
    productToUpdate.Brand = productDTO.Brand;
    productToUpdate.CategoryId = productDTO.CategoryId;

    db.SaveChanges();
    return Results.NoContent();
});


//C. ORDER ENDPOINTS
//1. GET 1 Order w/ EVERYTHING
app.MapGet("/api/orders/{id}", (CornerStoreDbContext db, int id) =>
{
    OrderDTO orderDTO = db.Orders
        .Include(o => o.Cashier)
        .Include(o => o.OrderProducts)
            .ThenInclude(op => op.Product)
                .ThenInclude(p => p.Category)
        .Where(o => o.Id == id)
        .Select(o => new OrderDTO
        {
            Id = o.Id,
            CashierId = o.CashierId,
            PaidOnDate = o.PaidOnDate,
            Cashier = new CashierDTO
            {
                Id = o.Cashier.Id,
                FirstName = o.Cashier.FirstName,
                LastName = o.Cashier.LastName
            },
            OrderProducts = o.OrderProducts.Select(op => new OrderProductDTO
            {
                Id = op.Id,
                OrderId = op.OrderId,
                ProductId = op.ProductId,
                Quantity = op.Quantity,
                Product = new ProductDTO
                {
                    Id = op.Product.Id,
                    ProductName = op.Product.ProductName,
                    Price = op.Product.Price,
                    Brand = op.Product.Brand,
                    CategoryId = op.Product.CategoryId,
                    Category = new CategoryDTO
                    {
                        Id = op.Product.Category.Id,
                        CategoryName = op.Product.Category.CategoryName
                    }
                }
            }).ToList()
        })
        .SingleOrDefault();

    if (orderDTO == null)
    {
        return Results.NotFound();
    }

    return Results.Ok(orderDTO);
});

//2. GET All Orders w/ Opt. Query String Param Day Filter
// TEST w/ http://localhost:5000/api/orders?orderDate=2025-05-20
app.MapGet("/api/orders", (CornerStoreDbContext db, DateTime? orderDate) =>
{
    IQueryable<Order> query = db.Orders;

    if (orderDate.HasValue) // check if there's an orderDate provided
    {
        DateTime startDate = orderDate.Value.Date; // define start of day
        DateTime endDate = startDate.AddDays(1); // define start of next day

        query = query.Where(o => o.PaidOnDate >= startDate && o.PaidOnDate < endDate);
    }

    List<OrderDTO> orders = query
        .Select(o => new OrderDTO
        {
            Id = o.Id,
            CashierId = o.CashierId,
            PaidOnDate = o.PaidOnDate
        }).ToList();

    return Results.Ok(orders);
});

//3. DELETE 1 Order
app.MapDelete("/api/orders/{id}", (CornerStoreDbContext db, int id) =>
{
    Order order = db.Orders.SingleOrDefault(order => order.Id == id);
    if (order == null)
    {
        return Results.NotFound();
    }
    db.Orders.Remove(order);
    db.SaveChanges();
    return Results.NoContent();
});

//4. POST Order w/ Products
/*
{
  "cashierId": 1,
  "paidOnDate": "2025-05-23T10:00:00",
  "orderProducts": [
    { "productId": 1, "quantity": 2 },
    { "productId": 3, "quantity": 1 }
  ]
}
*/
app.MapPost("/api/orders", (CornerStoreDbContext db, OrderDTO newOrderDTO) =>
{
    // Create and save the Order
    Order newOrder = new Order
    {
        CashierId = newOrderDTO.CashierId,
        PaidOnDate = newOrderDTO.PaidOnDate,
        OrderProducts = new List<OrderProduct>()
    };

    db.Orders.Add(newOrder);
    db.SaveChanges();

    // Add OrderProducts
    if (newOrderDTO.OrderProducts != null)
    {
        foreach (OrderProductDTO opDTO in newOrderDTO.OrderProducts)
        {
            OrderProduct orderProduct = new OrderProduct
            {
                OrderId = newOrder.Id,
                ProductId = opDTO.ProductId,
                Quantity = opDTO.Quantity
            };
            db.OrderProducts.Add(orderProduct);
        }
        db.SaveChanges();
    }

    // Fetch the full OrderDTO to return
    OrderDTO createdOrder = db.Orders
        .Include(o => o.Cashier)
        .Include(o => o.OrderProducts)
            .ThenInclude(op => op.Product)
                .ThenInclude(p => p.Category)
        .Where(o => o.Id == newOrder.Id)
        .Select(o => new OrderDTO
        {
            Id = o.Id,
            CashierId = o.CashierId,
            PaidOnDate = o.PaidOnDate,
            Cashier = new CashierDTO
            {
                Id = o.Cashier.Id,
                FirstName = o.Cashier.FirstName,
                LastName = o.Cashier.LastName
            },
            OrderProducts = o.OrderProducts.Select(op => new OrderProductDTO
            {
                Id = op.Id,
                ProductId = op.ProductId,
                OrderId = op.OrderId,
                Quantity = op.Quantity,
                Product = new ProductDTO
                {
                    Id = op.Product.Id,
                    ProductName = op.Product.ProductName,
                    Price = op.Product.Price,
                    Brand = op.Product.Brand,
                    CategoryId = op.Product.CategoryId,
                    Category = new CategoryDTO
                    {
                        Id = op.Product.Category.Id,
                        CategoryName = op.Product.Category.CategoryName
                    }
                }
            }).ToList()
        }).Single();

    return Results.Created($"/api/orders/{createdOrder.Id}", createdOrder);
});


app.Run();

//don't move or change this!
public partial class Program { }