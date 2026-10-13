using Microsoft.EntityFrameworkCore;
using StokTakip.Api.Models;

namespace StokTakip.Api.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext db)
    {
        if (await db.Categories.AnyAsync()) return;

        var names = new[] { "Elektronik", "Giyim", "Gıda", "Kırtasiye", "Temizlik" };
        var categories = names.Select(n => new Category { Name = n }).ToList();
        db.Categories.AddRange(categories);
        await db.SaveChangesAsync();

        var random = new Random(42);
        var products = Enumerable.Range(1, 50).Select(i => new Product
        {
            CategoryId = categories[random.Next(categories.Count)].Id,
            Name = $"Ürün {i:00}",
            Sku = $"SKU-{i:0000}",
            Quantity = random.Next(0, 201),
            Price = Math.Round((decimal)(random.NextDouble() * 1995 + 5), 2),
            MinStock = random.Next(5, 31),
        });
        db.Products.AddRange(products);
        await db.SaveChangesAsync();
    }
}