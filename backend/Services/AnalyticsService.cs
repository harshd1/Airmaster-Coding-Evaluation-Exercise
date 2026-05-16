namespace AirmasterOrderApi.Services;

using AirmasterOrderApi.Models;

/// <summary>
/// Analytics service for tracking user events, conversions, and system metrics.
/// Provides data for admin dashboards and business intelligence.
/// Implements in-memory storage for this demo; in production, use Azure Data Explorer or similar.
/// </summary>
public class AnalyticsService
{
    private readonly List<AnalyticsEvent> _events = new();
    private readonly object _lock = new object();

    public void LogEvent(AnalyticsEvent @event)
    {
        if (@event == null) return;

        lock (_lock)
        {
            @event.EventId = Guid.NewGuid();
            _events.Add(@event);
        }
    }

    public AnalyticsStats GetAnalyticsStats()
    {
        lock (_lock)
        {
            var stats = new AnalyticsStats
            {
                TotalEvents = _events.Count,
                TotalProductBrowses = _events.Count(e => e.EventType == "ProductBrowse"),
                TotalOrdersCreated = _events.Count(e => e.EventType == "OrderCreated"),
                PaymentSuccesses = _events.Count(e => e.EventType == "PaymentSuccess"),
                PaymentFailures = _events.Count(e => e.EventType == "PaymentFailed"),
                ConversionRate = CalculateConversionRate(),
                RecentEvents = _events.OrderByDescending(e => e.Timestamp).Take(10).ToList()
            };

            return stats;
        }
    }

    private decimal CalculateConversionRate()
    {
        lock (_lock)
        {
            var browses = _events.Count(e => e.EventType == "ProductBrowse");
            var orders = _events.Count(e => e.EventType == "OrderCreated");
            
            return browses > 0 ? (decimal)orders / browses * 100 : 0;
        }
    }

    public List<AnalyticsEvent> GetEventsByType(string eventType)
    {
        lock (_lock)
        {
            return _events.Where(e => e.EventType == eventType).ToList();
        }
    }
}

public class AnalyticsEvent
{
    public Guid EventId { get; set; } = Guid.NewGuid();
    public Guid? UserId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string PageUrl { get; set; } = string.Empty;
    public Guid? ProductId { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string Metadata { get; set; } = string.Empty;
}

public class AnalyticsStats
{
    public int TotalEvents { get; set; }
    public int TotalProductBrowses { get; set; }
    public int TotalOrdersCreated { get; set; }
    public int PaymentSuccesses { get; set; }
    public int PaymentFailures { get; set; }
    public decimal ConversionRate { get; set; }
    public List<AnalyticsEvent> RecentEvents { get; set; } = new();
}
