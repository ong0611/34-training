using Microsoft.EntityFrameworkCore;
using OrderHub.Core.Domain;
using OrderHub.Core.Interfaces;
using OrderHub.Core.Services;
using OrderHub.Infrastructure.Data;

namespace OrderHub.Infrastructure.Repositories;

public class ProductRepository : IProductRepository
{
    private readonly OrderHubDbContext _db;

    public ProductRepository(OrderHubDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<Product>> GetAllAsync() =>
        await _db.Products.OrderBy(p => p.Sku).ToListAsync();

    public async Task<IReadOnlyList<Product>> GetActiveAsync() =>
        await _db.Products.Where(p => p.IsActive).OrderBy(p => p.Sku).ToListAsync();

    public Task<Product?> GetByIdAsync(int id) =>
        _db.Products.FirstOrDefaultAsync(p => p.Id == id);

    public async Task<IReadOnlyList<LowStockProductResult>> GetLowStockAsync(int threshold, DateTime since)
    {
        var query =
            from p in _db.Products
            where p.IsActive && p.StockQuantity < threshold
            orderby p.StockQuantity
            select new LowStockProductResult
            {
                Sku = p.Sku,
                Name = p.Name,
                StockQuantity = p.StockQuantity,
                RecentSalesQuantity = _db.OrderItems
                    .Where(oi => oi.ProductId == p.Id
                        && oi.Order!.Status != OrderStatus.Cancelled
                        && oi.Order.CreatedAt >= since)
                    .Sum(oi => (int?)oi.Quantity) ?? 0
            };

        return await query.ToListAsync();
    }

    public Task SaveChangesAsync() => _db.SaveChangesAsync();
}
