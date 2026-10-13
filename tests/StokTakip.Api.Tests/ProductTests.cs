using System.Net;
using System.Net.Http.Json;
using StokTakip.Api.Dtos;
using Xunit;

namespace StokTakip.Api.Tests;

public class ProductTests : ApiTestBase
{
    [Fact]
    public async Task Create_returns_201_and_the_product()
    {
        var categoryId = await AddCategoryAsync();

        var response = await Client.PostAsJsonAsync("/api/products", NewProduct(categoryId, "TEST-0001"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var product = await response.Content.ReadFromJsonAsync<ProductResponse>();
        Assert.Equal("TEST-0001", product!.Sku);
    }

    [Fact]
    public async Task Duplicate_sku_is_rejected_with_409()
    {
        var categoryId = await AddCategoryAsync();
        await AddProductAsync(categoryId, "TEST-0001");

        var response = await Client.PostAsJsonAsync("/api/products", NewProduct(categoryId, "TEST-0001"));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var list = await Client.GetFromJsonAsync<PagedResult<ProductResponse>>("/api/products");
        Assert.Equal(1, list!.TotalCount);
    }

    [Fact]
    public async Task Negative_quantity_is_rejected_with_400()
    {
        var categoryId = await AddCategoryAsync();

        var response = await Client.PostAsJsonAsync("/api/products", NewProduct(categoryId, "TEST-0001", quantity: -5));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var list = await Client.GetFromJsonAsync<PagedResult<ProductResponse>>("/api/products");
        Assert.Equal(0, list!.TotalCount);
    }

    [Fact]
    public async Task Unknown_category_is_rejected_with_400()
    {
        var response = await Client.PostAsJsonAsync("/api/products", NewProduct(999, "TEST-0001"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Update_changes_the_product()
    {
        var categoryId = await AddCategoryAsync();
        var id = await AddProductAsync(categoryId, "OLD-0001");

        var response = await Client.PutAsJsonAsync($"/api/products/{id}", NewProduct(categoryId, "OLD-0001", name: "Yeni Ad"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var product = await Client.GetFromJsonAsync<ProductResponse>($"/api/products/{id}");
        Assert.Equal("Yeni Ad", product!.Name);
    }

    [Fact]
    public async Task Product_can_keep_its_own_sku_when_updated()
    {
        var categoryId = await AddCategoryAsync();
        var id = await AddProductAsync(categoryId, "KEEP-0001");

        var response = await Client.PutAsJsonAsync($"/api/products/{id}", NewProduct(categoryId, "KEEP-0001"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Sku_of_another_product_is_rejected_on_update()
    {
        var categoryId = await AddCategoryAsync();
        await AddProductAsync(categoryId, "TAKEN-0001");
        var id = await AddProductAsync(categoryId, "MINE-0001");

        var response = await Client.PutAsJsonAsync($"/api/products/{id}", NewProduct(categoryId, "TAKEN-0001"));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Delete_removes_the_product()
    {
        var categoryId = await AddCategoryAsync();
        var id = await AddProductAsync(categoryId, "DEL-0001");

        var response = await Client.DeleteAsync($"/api/products/{id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var after = await Client.GetAsync($"/api/products/{id}");
        Assert.Equal(HttpStatusCode.NotFound, after.StatusCode);
    }

    [Fact]
    public async Task Products_can_be_searched_by_name()
    {
        var categoryId = await AddCategoryAsync();
        await AddProductAsync(categoryId, "A-1", name: "Kırmızı Kalem");
        await AddProductAsync(categoryId, "A-2", name: "Mavi Defter");

        var list = await Client.GetFromJsonAsync<PagedResult<ProductResponse>>("/api/products?search=Kalem");

        Assert.Single(list!.Items);
        Assert.Equal("Kırmızı Kalem", list.Items[0].Name);
    }

    [Fact]
    public async Task Products_can_be_filtered_by_category()
    {
        var first = await AddCategoryAsync("Birinci");
        var second = await AddCategoryAsync("İkinci");
        await AddProductAsync(first, "B-1", name: "Birinci Ürün");
        await AddProductAsync(second, "B-2", name: "İkinci Ürün");

        var list = await Client.GetFromJsonAsync<PagedResult<ProductResponse>>($"/api/products?categoryId={first}");

        Assert.Single(list!.Items);
        Assert.Equal("Birinci Ürün", list.Items[0].Name);
    }

    [Fact]
    public async Task Low_stock_products_are_flagged()
    {
        var categoryId = await AddCategoryAsync();
        await AddProductAsync(categoryId, "LOW-1", quantity: 2, minStock: 10);
        await AddProductAsync(categoryId, "OK-1", quantity: 50, minStock: 5);

        var list = await Client.GetFromJsonAsync<PagedResult<ProductResponse>>("/api/products");

        Assert.True(list!.Items.Single(p => p.Sku == "LOW-1").IsLowStock);
        Assert.False(list.Items.Single(p => p.Sku == "OK-1").IsLowStock);
    }
}