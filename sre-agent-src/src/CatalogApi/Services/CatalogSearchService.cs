using CatalogApi.Models;

namespace CatalogApi.Services;

public class CatalogSearchService
{
    /// <summary>
    /// Filters and sorts products for search results:
    /// out-of-stock items always appear at the bottom, and can optionally be hidden entirely.
    /// </summary>
    public IEnumerable<Product> FilterAndSort(IEnumerable<Product> products, bool hideOutOfStock)
    {
        var filtered = hideOutOfStock
            ? products.Where(p => p.AvailableStock > 0)
            : products;

        // Out-of-stock items (AvailableStock <= 0) are sorted to the bottom.
        return filtered.OrderBy(p => p.AvailableStock <= 0 ? 1 : 0);
    }
}
