using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

if (args.Length != 1)
{
    Console.Error.WriteLine("Invalid arguments!");
}
else
{
    var factory = new OrderSystemContextFactory();
    using var context = factory.CreateDbContext(args);

    Manager manager = new Manager(context);

    switch (args[0])
    {
        //Importing data from files
        case "import":
            await manager.Import(args);
            break;

        //Removing all rows in the tables "Customer" and "Orders"
        case "clean":
            await manager.Clean();
            break;

        case "check":
            await manager.Check();
            break;

        //First clearing the database then importing data and searching for limit exceeds
        case "full":
            await manager.Clean();
            await manager.Import(args);
            await manager.Check();
            break;
        default:
            break;
    }

    
}
class Manager
{
    public Manager(OrderSystemContext context)
    {
        Context = context;
    }

    public OrderSystemContext Context { get; }

    public async Task Import(string[] args)
    {
        var customerLines = await File.ReadAllLinesAsync("customers.txt");
        var orderLines = await File.ReadAllLinesAsync("orders.txt");

        foreach (var customer in customerLines.Skip(1))
        {
            var splitLine = customer.Split("\t");
            await Context.AddAsync(new Customer { Name = splitLine[0], CreditLimit = decimal.Parse(splitLine[1]) });
        }
        await Context.SaveChangesAsync();
        Console.WriteLine("Added customers");

        foreach (var order in orderLines.Skip(1))
        {
            var splitLine = order.Split("\t");
            await Context.AddAsync(new Order
            {
                CustomerId = Context.Customer.Where(c => c.Name == splitLine[0]).ToArray()[0].Id,
                OrderDate = DateTime.Parse(splitLine[1]),
                OrderValue = decimal.Parse(splitLine[2])
            });
        }
        await Context.SaveChangesAsync();
        Console.WriteLine("Added orders");
    }

    public async Task Clean()
    {
        foreach (var customer in Context.Customer)
        {
            Context.Remove(customer);
        }
        foreach (var order in Context.Orders)
        {
            Context.Remove(order);
        }
        await Context.SaveChangesAsync();
        Console.WriteLine("Cleaned the database!");
    }

    public async Task Check()
    {
        var orders = await Context.Orders.ToListAsync();
        foreach (var customer in Context.Customer)
        {
            var orderSum = orders.Where(c => customer.Id == c.CustomerId).Sum(o => o.OrderValue);
            if (orderSum > customer.CreditLimit)
            {
                Console.WriteLine($"The customer {customer.Name} with ID {customer.Id} exceeded his limit of {customer.CreditLimit} by {orderSum - customer.CreditLimit}");
            }
        }
    }
}

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