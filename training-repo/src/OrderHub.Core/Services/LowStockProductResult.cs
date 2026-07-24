namespace OrderHub.Core.Services;

public class LowStockProductResult
{
    public string Sku { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int StockQuantity { get; set; }
    public int RecentSalesQuantity { get; set; }
}
