using AirmasterOrderApi.Models;

namespace AirmasterOrderApi.Services;

public class OrderService
{
    private readonly List<Product> _products = new()
    {
        new Product { ProductId = Guid.NewGuid(), Name = "Airmaster Running Shoes", Description = "High-performance shoes for urban runners.", Price = 129.99m, Category = "Footwear", StockQuantity = 200, ImageUrl = "https://example.com/images/shoes.png", UpdatedAt = DateTime.UtcNow },
        new Product { ProductId = Guid.NewGuid(), Name = "Airmaster Backpack", Description = "Durable travel backpack with smart compartments.", Price = 89.99m, Category = "Accessories", StockQuantity = 140, ImageUrl = "https://example.com/images/backpack.png", UpdatedAt = DateTime.UtcNow }
    };

    private readonly List<Order> _orders = new();

    public IEnumerable<Product> GetProducts() => _products;

    public Product? GetProduct(Guid id) => _products.FirstOrDefault(p => p.ProductId == id);

    public Order? GetOrder(Guid id) => _orders.FirstOrDefault(o => o.OrderId == id);

    public Order CreateOrder(OrderRequest request)
    {
        var order = new Order
        {
            OrderId = Guid.NewGuid(),
            UserId = request.UserId,
            CreatedAt = DateTime.UtcNow,
            Status = OrderStatus.Pending,
            ShippingAddress = request.ShippingAddress,
            BillingAddress = request.BillingAddress,
            Items = request.Items.ToList(),
            TotalAmount = request.Items.Sum(item => item.UnitPrice * item.Quantity)
        };

        _orders.Add(order);
        return order;
    }

    public PaymentResult ProcessPayment(PaymentRequest request)
    {
        var order = GetOrder(request.OrderId);
        if (order is null)
        {
            return new PaymentResult { Success = false, Message = "Order not found." };
        }

        // Simulated payment integration logic
        if (request.Amount <= 0)
        {
            return new PaymentResult { Success = false, Message = "Invalid payment amount." };
        }

        order.Status = OrderStatus.Paid;
        return new PaymentResult { Success = true, Message = "Payment successful.", TransactionId = Guid.NewGuid().ToString() };
    }
}
