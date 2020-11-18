using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.IO;
using System.Linq;


    var factory = new OrderSystemContextFactory();
    using var context = factory.CreateDbContext(args);

    var customerLines = await File.ReadAllLinesAsync("customers.txt");
    var orderLines = await File.ReadAllLinesAsync("orders.txt");

    foreach (var customer in customerLines.Skip(1))
    {
        var splitLine = customer.Split("\t");
        await context.AddAsync(new Customer { Name = splitLine[0], CreditLimit = decimal.Parse(splitLine[1]) });
    }
await context.SaveChangesAsync();
Console.WriteLine("Added customers");

foreach (var order in orderLines.Skip(1))
{
    var splitLine = order.Split("\t");
    await context.AddAsync(new Order { CustomerId = context.Customer.Where(c => c.Name == splitLine[0]).ToArray()[0].Id, 
        OrderDate = DateTime.Parse(splitLine[1]), OrderValue = decimal.Parse(splitLine[2]) });
}
await context.SaveChangesAsync();
Console.WriteLine("Added orders");


//Create the model class

class Customer
{
    public int Id { get; set; }
    [MaxLength(100)]
    public string Name { get; set; } = "default";
    [Column(TypeName = "decimal(8, 2)")]
    public decimal CreditLimit { get; set; }
    public List<Order>? Orders { get; set; }
}

class Order
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public DateTime OrderDate { get; set; }
    [Column(TypeName = "decimal(8, 2)")]
    public decimal OrderValue { get; set; }
}

class OrderSystemContext : DbContext
{
    public DbSet<Customer> Customer { get; set; }
    public DbSet<Order> Orders { get; set; }
#pragma warning disable CS8618 // Ein Non-Nullable-Feld muss beim Beenden des Konstruktors einen Wert ungleich NULL enthalten. Erwägen Sie die Deklaration als Nullable.
    public OrderSystemContext(DbContextOptions<OrderSystemContext> options)
#pragma warning restore CS8618 // Ein Non-Nullable-Feld muss beim Beenden des Konstruktors einen Wert ungleich NULL enthalten. Erwägen Sie die Deklaration als Nullable.
        : base(options)
    { }
}

class OrderSystemContextFactory : IDesignTimeDbContextFactory<OrderSystemContext>
{
    public OrderSystemContext CreateDbContext(string[]? args = null)
    {
        var configuration = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();

        var optionsBuilder = new DbContextOptionsBuilder<OrderSystemContext>();
        optionsBuilder
            // Uncomment the following line if you want to print generated
            // SQL statements on the console.
            // .UseLoggerFactory(LoggerFactory.Create(builder => builder.AddConsole()))
            .UseSqlServer(configuration["ConnectionStrings:DefaultConnection"]);

        return new OrderSystemContext(optionsBuilder.Options);
    }
}