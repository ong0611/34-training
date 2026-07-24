using OrderHub.Core.Domain;

namespace OrderHub.Tests;

public class ProductServiceLowStockTests
{
    [Fact]
    public async Task GetLowStock_FiltersByThresholdAndSortsAscending()
    {
        using var db = TestSetup.CreateContext();
        var service = TestSetup.CreateProductService(db);
        TestSetup.AddProduct(db, stock: 3, sku: "LOW");
        TestSetup.AddProduct(db, stock: 8, sku: "MID");
        TestSetup.AddProduct(db, stock: 10, sku: "EQ");
        TestSetup.AddProduct(db, stock: 20, sku: "HIGH");

        var result = await service.GetLowStockAsync(10);

        Assert.Equal(new[] { "LOW", "MID" }, result.Select(r => r.Sku));
    }

    [Fact]
    public async Task GetLowStock_ExcludesInactiveProducts()
    {
        using var db = TestSetup.CreateContext();
        var service = TestSetup.CreateProductService(db);
        TestSetup.AddProduct(db, stock: 2, isActive: false, sku: "INACTIVE");
        TestSetup.AddProduct(db, stock: 2, sku: "ACTIVE");

        var result = await service.GetLowStockAsync(10);

        Assert.Single(result);
        Assert.Equal("ACTIVE", result[0].Sku);
    }

    [Fact]
    public async Task GetLowStock_RecentSalesExcludesCancelledAndOldOrders()
    {
        using var db = TestSetup.CreateContext();
        var service = TestSetup.CreateProductService(db);
        var customer = TestSetup.AddCustomer(db);
        var product = TestSetup.AddProduct(db, stock: 2, sku: "SKU1");

        var recentOrder = new Order { CustomerId = customer.Id, Status = OrderStatus.Confirmed, CreatedAt = DateTime.UtcNow.AddDays(-5) };
        recentOrder.Items.Add(new OrderItem { ProductId = product.Id, Quantity = 4, UnitPriceSnapshot = 100m });

        var cancelledOrder = new Order { CustomerId = customer.Id, Status = OrderStatus.Cancelled, CreatedAt = DateTime.UtcNow.AddDays(-5) };
        cancelledOrder.Items.Add(new OrderItem { ProductId = product.Id, Quantity = 99, UnitPriceSnapshot = 100m });

        var oldOrder = new Order { CustomerId = customer.Id, Status = OrderStatus.Confirmed, CreatedAt = DateTime.UtcNow.AddDays(-40) };
        oldOrder.Items.Add(new OrderItem { ProductId = product.Id, Quantity = 77, UnitPriceSnapshot = 100m });

        db.Orders.AddRange(recentOrder, cancelledOrder, oldOrder);
        db.SaveChanges();

        var result = await service.GetLowStockAsync(10);

        Assert.Equal(4, result.Single(r => r.Sku == "SKU1").RecentSalesQuantity);
    }
}
