using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StokTakip.Api.Data;
using StokTakip.Api.Dtos;
using StokTakip.Api.Models;

namespace StokTakip.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CategoriesController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<CategoryResponse>>> GetAll() =>
        await db.Categories
            .OrderBy(c => c.Name)
            .Select(c => new CategoryResponse(c.Id, c.Name, c.Products.Count))
            .ToListAsync();

    [HttpPost]
    public async Task<ActionResult<CategoryResponse>> Create(CategoryRequest request)
    {
        if (await db.Categories.AnyAsync(c => c.Name == request.Name))
            return Conflict(new { error = "Bu kategori adı zaten var." });

        var category = new Category { Name = request.Name };
        db.Categories.Add(category);
        await db.SaveChangesAsync();

        return Created($"/api/categories/{category.Id}", new CategoryResponse(category.Id, category.Name, 0));
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<CategoryResponse>> Update(int id, CategoryRequest request)
    {
        var category = await db.Categories.FindAsync(id);
        if (category is null) return NotFound();

        if (await db.Categories.AnyAsync(c => c.Name == request.Name && c.Id != id))
            return Conflict(new { error = "Bu kategori adı zaten var." });

        category.Name = request.Name;
        await db.SaveChangesAsync();

        var count = await db.Products.CountAsync(p => p.CategoryId == id);
        return new CategoryResponse(category.Id, category.Name, count);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var category = await db.Categories.FindAsync(id);
        if (category is null) return NotFound();

        if (await db.Products.AnyAsync(p => p.CategoryId == id))
            return Conflict(new { error = "Bu kategoriye ait ürünler var, silinemez." });

        db.Categories.Remove(category);
        await db.SaveChangesAsync();
        return NoContent();
    }
}