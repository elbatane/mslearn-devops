using CatalogApi.Models;
using CatalogApi.Services;
using Xunit;

namespace CatalogApi.Tests;

public class CatalogSearchServiceTests
{
    private readonly CatalogSearchService _service = new();

    [Fact]
    public void FilterAndSort_OutOfStockItemsAppearAtBottom()
    {
        var products = new List<Product>
        {
            new() { Name = "A", AvailableStock = 0 },
            new() { Name = "B", AvailableStock = 10 },
            new() { Name = "C", AvailableStock = 0 },
            new() { Name = "D", AvailableStock = 5 },
        };

        var result = _service.FilterAndSort(products, hideOutOfStock: false).ToList();

        // In-stock items come first
        Assert.True(result[0].AvailableStock > 0);
        Assert.True(result[1].AvailableStock > 0);
        // Out-of-stock items are at the bottom
        Assert.Equal(0, result[2].AvailableStock);
        Assert.Equal(0, result[3].AvailableStock);
    }

    [Fact]
    public void FilterAndSort_HideOutOfStock_RemovesOutOfStockItems()
    {
        var products = new List<Product>
        {
            new() { Name = "A", AvailableStock = 0 },
            new() { Name = "B", AvailableStock = 10 },
            new() { Name = "C", AvailableStock = 0 },
        };

        var result = _service.FilterAndSort(products, hideOutOfStock: true).ToList();

        Assert.Single(result);
        Assert.Equal("B", result[0].Name);
    }

    [Fact]
    public void FilterAndSort_ShowOutOfStock_KeepsAllItems()
    {
        var products = new List<Product>
        {
            new() { Name = "A", AvailableStock = 0 },
            new() { Name = "B", AvailableStock = 10 },
        };

        var result = _service.FilterAndSort(products, hideOutOfStock: false).ToList();

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public void FilterAndSort_AllInStock_OrderUnchangedByStockFlag()
    {
        var products = new List<Product>
        {
            new() { Name = "A", AvailableStock = 5 },
            new() { Name = "B", AvailableStock = 10 },
        };

        var result = _service.FilterAndSort(products, hideOutOfStock: false).ToList();

        Assert.Equal(2, result.Count);
        Assert.All(result, p => Assert.True(p.AvailableStock > 0));
    }

    [Fact]
    public void FilterAndSort_AllOutOfStock_HideOutOfStock_ReturnsEmpty()
    {
        var products = new List<Product>
        {
            new() { Name = "A", AvailableStock = 0 },
            new() { Name = "B", AvailableStock = 0 },
        };

        var result = _service.FilterAndSort(products, hideOutOfStock: true).ToList();

        Assert.Empty(result);
    }

    [Fact]
    public void FilterAndSort_EmptyList_ReturnsEmpty()
    {
        var result = _service.FilterAndSort(Enumerable.Empty<Product>(), hideOutOfStock: false).ToList();

        Assert.Empty(result);
    }
}
