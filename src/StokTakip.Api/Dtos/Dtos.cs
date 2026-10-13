using System.ComponentModel.DataAnnotations;

namespace StokTakip.Api.Dtos;

public class CategoryRequest
{
    [Required, StringLength(255)]
    public string Name { get; set; } = string.Empty;
}

public class ProductRequest
{
    [Range(1, int.MaxValue)]
    public int CategoryId { get; set; }

    [Required, StringLength(255)]
    public string Name { get; set; } = string.Empty;

    [Required, StringLength(50)]
    public string Sku { get; set; } = string.Empty;

    [Range(0, int.MaxValue)]
    public int Quantity { get; set; }

    [Range(0, 99999999.99)]
    public decimal Price { get; set; }

    [Range(0, int.MaxValue)]
    public int MinStock { get; set; }
}

public record CategoryResponse(int Id, string Name, int ProductCount);

public record ProductResponse(
    int Id, int CategoryId, string CategoryName, string Name, string Sku,
    int Quantity, decimal Price, int MinStock, bool IsLowStock);

public record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount);