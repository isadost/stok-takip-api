using System.Net;
using System.Net.Http.Json;
using StokTakip.Api.Dtos;
using Xunit;

namespace StokTakip.Api.Tests;

public class CategoryTests : ApiTestBase
{
    [Fact]
    public async Task Create_returns_201()
    {
        var response = await Client.PostAsJsonAsync("/api/categories", new { name = "Oyuncak" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Duplicate_name_is_rejected_with_409()
    {
        await AddCategoryAsync("Oyuncak");

        var response = await Client.PostAsJsonAsync("/api/categories", new { name = "Oyuncak" });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Category_with_products_cannot_be_deleted()
    {
        var categoryId = await AddCategoryAsync();
        await AddProductAsync(categoryId, "P-1");

        var response = await Client.DeleteAsync($"/api/categories/{categoryId}");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var categories = await Client.GetFromJsonAsync<List<CategoryResponse>>("/api/categories");
        Assert.Contains(categories!, c => c.Id == categoryId);
    }

    [Fact]
    public async Task Empty_category_can_be_deleted()
    {
        var categoryId = await AddCategoryAsync();

        var response = await Client.DeleteAsync($"/api/categories/{categoryId}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }
}