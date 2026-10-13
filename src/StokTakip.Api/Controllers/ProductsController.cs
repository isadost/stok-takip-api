using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StokTakip.Api.Data;
using StokTakip.Api.Dtos;
using StokTakip.Api.Models;

namespace StokTakip.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<ProductResponse>>> GetAll(
        string? search, int? categoryId, int page = 1, int pageSize = 10)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = db.Products.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = $"%{search.Trim()}%";
            query = query.Where(p => EF.Functions.Like(p.Name, term) || EF.Functions.Like(p.Sku, term));
        }

        if (categoryId.HasValue)
            query = query.Where(p => p.CategoryId == categoryId.Value);

        var total = await query.CountAsync();

        var items = await query
            .OrderByDescending(p => p.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new ProductResponse(
                p.Id, p.CategoryId, p.Category!.Name, p.Name, p.Sku,
                p.Quantity, p.Price, p.MinStock, p.Quantity <= p.MinStock))
            .ToListAsync();

        return new PagedResult<ProductResponse>(items, page, pageSize, total);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ProductResponse>> GetById(int id)
    {
        var product = await db.Products.Include(p => p.Category).FirstOrDefaultAsync(p => p.Id == id);
        return product is null ? NotFound() : ToResponse(product);
    }

    [HttpPost]
    public async Task<ActionResult<ProductResponse>> Create(ProductRequest request)
    {
        if (!await db.Categories.AnyAsync(c => c.Id == request.CategoryId))
            return BadRequest(new { error = "Kategori bulunamadı." });

        if (await db.Products.AnyAsync(p => p.Sku == request.Sku))
            return Conflict(new { error = "Bu SKU zaten kullanılıyor." });

        var product = new Product
        {
            CategoryId = request.CategoryId,
            Name = request.Name,
            Sku = request.Sku,
            Quantity = request.Quantity,
            Price = request.Price,
            MinStock = request.MinStock,
        };
        db.Products.Add(product);
        await db.SaveChangesAsync();
        await db.Entry(product).Reference(p => p.Category).LoadAsync();

        return CreatedAtAction(nameof(GetById), new { id = product.Id }, ToResponse(product));
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ProductResponse>> Update(int id, ProductRequest request)
    {
        var product = await db.Products.FindAsync(id);
        if (product is null) return NotFound();

        if (!await db.Categories.AnyAsync(c => c.Id == request.CategoryId))
            return BadRequest(new { error = "Kategori bulunamadı." });

        if (await db.Products.AnyAsync(p => p.Sku == request.Sku && p.Id != id))
            return Conflict(new { error = "Bu SKU zaten kullanılıyor." });

        product.CategoryId = request.CategoryId;
        product.Name = request.Name;
        product.Sku = request.Sku;
        product.Quantity = request.Quantity;
        product.Price = request.Price;
        product.MinStock = request.MinStock;
        await db.SaveChangesAsync();
        await db.Entry(product).Reference(p => p.Category).LoadAsync();

        return ToResponse(product);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var product = await db.Products.FindAsync(id);
        if (product is null) return NotFound();

        db.Products.Remove(product);
        await db.SaveChangesAsync();
        return NoContent();
    }

    private static ProductResponse ToResponse(Product p) => new(
        p.Id, p.CategoryId, p.Category?.Name ?? string.Empty, p.Name, p.Sku,
        p.Quantity, p.Price, p.MinStock, p.Quantity <= p.MinStock);
}