namespace AirmasterOrderApi.Services;

/// <summary>
/// Rate limiting service to protect API endpoints from abuse and ensure fair resource usage.
/// Implements a sliding window counter approach with configurable limits per client.
/// </summary>
public class RateLimiter
{
    private readonly Dictionary<string, ClientRateLimit> _clientLimits = new();
    private readonly object _lock = new object();
    private const int RequestsPerMinute = 100;
    private const int WindowSeconds = 60;

    public bool IsAllowed(string clientId)
    {
        if (string.IsNullOrWhiteSpace(clientId))
            return false;

        lock (_lock)
        {
            var now = DateTime.UtcNow;

            if (!_clientLimits.ContainsKey(clientId))
            {
                _clientLimits[clientId] = new ClientRateLimit();
            }

            var limit = _clientLimits[clientId];

            // Remove old requests outside the window
            limit.Requests = limit.Requests
                .Where(timestamp => (now - timestamp).TotalSeconds < WindowSeconds)
                .ToList();

            // Check if client has exceeded the limit
            if (limit.Requests.Count >= RequestsPerMinute)
            {
                return false;
            }

            // Add current request
            limit.Requests.Add(now);
            limit.LastRequestTime = now;

            return true;
        }
    }

    public (int CurrentCount, int Limit, int RemainingSeconds) GetClientStatus(string clientId)
    {
        lock (_lock)
        {
            if (!_clientLimits.ContainsKey(clientId))
            {
                return (0, RequestsPerMinute, WindowSeconds);
            }

            var limit = _clientLimits[clientId];
            var now = DateTime.UtcNow;

            // Remove old requests
            limit.Requests = limit.Requests
                .Where(timestamp => (now - timestamp).TotalSeconds < WindowSeconds)
                .ToList();

            var oldestRequest = limit.Requests.FirstOrDefault();
            var remainingSeconds = oldestRequest != default 
                ? WindowSeconds - (int)(now - oldestRequest).TotalSeconds 
                : WindowSeconds;

            return (limit.Requests.Count, RequestsPerMinute, Math.Max(0, remainingSeconds));
        }
    }

    private class ClientRateLimit
    {
        public List<DateTime> Requests { get; set; } = new();
        public DateTime LastRequestTime { get; set; }
    }
}
