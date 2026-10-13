using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StokTakip.Api.Data;
using StokTakip.Api.Models;

namespace StokTakip.Api.Tests;

public class ApiFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");

    public ApiFactory()
    {
        _connection.Open();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureServices(services =>
        {
            var old = services.Where(d =>
                d.ServiceType == typeof(DbContextOptions<AppDbContext>) ||
                (d.ServiceType.IsGenericType
                    && d.ServiceType.GetGenericArguments().Contains(typeof(AppDbContext))
                    && d.ServiceType.Name.StartsWith("IDbContextOptionsConfiguration")))
                .ToList();
            foreach (var d in old) services.Remove(d);

            services.AddDbContext<AppDbContext>(o => o.UseSqlite(_connection));
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing) _connection.Dispose();
    }
}

public abstract class ApiTestBase : IDisposable
{
    protected readonly ApiFactory Factory = new();
    protected readonly HttpClient Client;

    protected ApiTestBase()
    {
        Client = Factory.CreateClient();
        using var scope = Factory.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.EnsureCreated();
    }

    protected async Task<int> AddCategoryAsync(string name = "Test Kategori")
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var category = new Category { Name = name };
        db.Categories.Add(category);
        await db.SaveChangesAsync();
        return category.Id;
    }

    protected async Task<int> AddProductAsync(
        int categoryId, string sku, string name = "Test Ürün", int quantity = 10, int minStock = 5)
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var product = new Product
        {
            CategoryId = categoryId, Name = name, Sku = sku,
            Quantity = quantity, Price = 10m, MinStock = minStock,
        };
        db.Products.Add(product);
        await db.SaveChangesAsync();
        return product.Id;
    }

    protected static object NewProduct(int categoryId, string sku, string name = "Yeni Ürün", int quantity = 10) =>
        new { categoryId, name, sku, quantity, price = 10m, minStock = 5 };

    public void Dispose()
    {
        Client.Dispose();
        Factory.Dispose();
        GC.SuppressFinalize(this);
    }
}