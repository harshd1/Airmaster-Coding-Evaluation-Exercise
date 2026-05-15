using AirmasterOrderApi.Models;

namespace AirmasterOrderApi.Services;

public class OrderService
{
    private readonly List<Product> _products = new()
    {
        new Product { ProductId = Guid.NewGuid(), Name = "Airmaster Running Shoes", Description = "High-performance shoes for urban runners.", Price = 129.99m, Category = "Footwear", StockQuantity = 200, ImageUrl = "https://example.com/images/shoes.png", UpdatedAt = DateTime.UtcNow },
        new Product { ProductId = Guid.NewGuid(), Name = "Airmaster Backpack", Description = "Durable travel backpack with smart compartments.", Price = 89.99m, Category = "Accessories", StockQuantity = 140, ImageUrl = "https://example.com/images/backpack.png", UpdatedAt = DateTime.UtcNow },
        new Product { ProductId = Guid.NewGuid(), Name = "Airmaster Travel Jacket", Description = "Water-resistant jacket designed for comfort and performance.", Price = 159.99m, Category = "Apparel", StockQuantity = 120, ImageUrl = "https://example.com/images/jacket.png", UpdatedAt = DateTime.UtcNow }
    };

    private readonly List<Order> _orders = new();
    private readonly Dictionary<Guid, int> _paymentAttemptCounts = new();
    private readonly Random _random = new();

    public IEnumerable<Product> GetProducts() => _products.Where(p => p.IsActive);

    public Product? GetProduct(Guid id) => _products.FirstOrDefault(p => p.ProductId == id && p.IsActive);

    public IEnumerable<Order> GetOrdersByUser(Guid userId) => _orders.Where(o => o.UserId == userId);

    public Order? GetOrder(Guid id) => _orders.FirstOrDefault(o => o.OrderId == id);

    public bool TryCreateOrder(OrderRequest request, out Order order, out string error)
    {
        order = null!;
        if (request.Items is null || !request.Items.Any())
        {
            error = "Order must contain at least one item.";
            return false;
        }

        var items = new List<OrderItem>();
        foreach (var item in request.Items)
        {
            var product = GetProduct(item.ProductId);
            if (product is null)
            {
                error = $"Product with ID {item.ProductId} was not found.";
                return false;
            }

            if (item.Quantity <= 0 || item.Quantity > product.StockQuantity)
            {
                error = $"Invalid quantity for product '{product.Name}'. Available stock: {product.StockQuantity}.";
                return false;
            }

            items.Add(new OrderItem(product.ProductId, item.Quantity, product.Price));
        }

        order = new Order
        {
            OrderId = Guid.NewGuid(),
            UserId = request.UserId == Guid.Empty ? Guid.NewGuid() : request.UserId,
            CreatedAt = DateTime.UtcNow,
            Status = OrderStatus.Pending,
            ShippingAddress = request.ShippingAddress,
            BillingAddress = request.BillingAddress,
            Items = items,
            TotalAmount = items.Sum(item => item.UnitPrice * item.Quantity)
        };

        _orders.Add(order);
        error = string.Empty;

        return true;
    }

    public PaymentResult ProcessPayment(PaymentRequest request)
    {
        var order = GetOrder(request.OrderId);
        if (order is null)
        {
            return new PaymentResult { Success = false, Message = "Order not found." };
        }

        if (order.Status != OrderStatus.Pending)
        {
            return new PaymentResult { Success = false, Message = "Payment cannot be processed for this order status." };
        }

        if (request.Amount <= 0 || request.Amount != order.TotalAmount)
        {
            return new PaymentResult { Success = false, Message = "Payment amount does not match order total." };
        }

        if (string.IsNullOrWhiteSpace(request.Token) || request.Token.Length < 8)
        {
            return new PaymentResult { Success = false, Message = "Invalid payment token. Please retry with valid payment details." };
        }

        // Simulated transient failure for retry logic.
        var attemptCount = _paymentAttemptCounts.GetValueOrDefault(order.OrderId); 
        if (attemptCount < 2 && _random.NextDouble() < 0.22)
        {
            _paymentAttemptCounts[order.OrderId] = attemptCount + 1;
            return new PaymentResult { Success = false, Message = "Payment gateway temporarily unavailable. Please retry.", TransactionId = null };
        }

        order.Status = OrderStatus.Paid;
        order.ShippingProvider = _random.Next(0, 2) == 0 ? "UPS" : "FedEx";
        order.TrackingNumber = GenerateTrackingNumber(order.ShippingProvider);

        return new PaymentResult
        {
            Success = true,
            Message = "Payment successful.",
            TransactionId = Guid.NewGuid().ToString()
        };
    }

    public bool TryShipOrder(Guid orderId, out Order? order, out string error)
    {
        order = GetOrder(orderId);
        if (order is null)
        {
            error = "Order not found.";
            return false;
        }

        if (order.Status != OrderStatus.Paid)
        {
            error = "Only paid orders can be shipped.";
            return false;
        }

        order.Status = OrderStatus.Shipped;
        order.TrackingNumber ??= GenerateTrackingNumber(order.ShippingProvider);
        order.ShippingProvider ??= _random.Next(0, 2) == 0 ? "UPS" : "FedEx";

        error = string.Empty;
        return true;
    }

    private static string GenerateTrackingNumber(string provider)
    {
        var prefix = provider.StartsWith("F", StringComparison.OrdinalIgnoreCase) ? "FDX" : "UPS";
        return $"{prefix}-{Guid.NewGuid().ToString("N").Substring(0, 12).ToUpperInvariant()}";
    }
}
