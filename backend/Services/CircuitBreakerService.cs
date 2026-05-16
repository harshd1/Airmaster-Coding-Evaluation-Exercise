namespace AirmasterOrderApi.Services;

using AirmasterOrderApi.Models;

/// <summary>
/// Circuit Breaker pattern implementation for fault-tolerant payment processing.
/// Implements retry logic with exponential backoff to handle transient failures.
/// </summary>
public class CircuitBreakerService
{
    private readonly Dictionary<string, CircuitState> _circuitStates = new();
    private const int FailureThreshold = 5;
    private const int TimeoutSeconds = 60;

    public enum CircuitStateEnum
    {
        Closed,      // Normal operation
        Open,        // Failures detected, rejecting requests
        HalfOpen     // Testing if service recovered
    }

    private class CircuitState
    {
        public CircuitStateEnum State { get; set; } = CircuitStateEnum.Closed;
        public int FailureCount { get; set; }
        public DateTime LastFailureTime { get; set; }
    }

    public PaymentResult ProcessPaymentWithRetry(Func<PaymentResult> paymentAction, int maxRetries)
    {
        const string circuitKey = "PaymentGateway";
        
        if (!_circuitStates.ContainsKey(circuitKey))
        {
            _circuitStates[circuitKey] = new CircuitState();
        }

        var state = _circuitStates[circuitKey];

        // Check if circuit should transition to HalfOpen
        if (state.State == CircuitStateEnum.Open && 
            DateTime.UtcNow.Subtract(state.LastFailureTime).TotalSeconds > TimeoutSeconds)
        {
            state.State = CircuitStateEnum.HalfOpen;
            state.FailureCount = 0;
        }

        // If circuit is Open (not HalfOpen), fail fast
        if (state.State == CircuitStateEnum.Open)
        {
            return new PaymentResult 
            { 
                Success = false, 
                Message = "Payment service temporarily unavailable. Please try again later." 
            };
        }

        // Attempt payment with retries and exponential backoff
        for (int attempt = 0; attempt < maxRetries; attempt++)
        {
            try
            {
                var result = paymentAction();

                if (result.Success)
                {
                    // Reset circuit state on success
                    state.State = CircuitStateEnum.Closed;
                    state.FailureCount = 0;
                    return result;
                }

                // Handle transient failures
                if (attempt < maxRetries - 1)
                {
                    int delayMs = (int)Math.Pow(2, attempt) * 1000; // Exponential backoff: 1s, 2s, 4s
                    Thread.Sleep(delayMs);
                }
            }
            catch (Exception ex)
            {
                state.FailureCount++;
                state.LastFailureTime = DateTime.UtcNow;

                if (state.FailureCount >= FailureThreshold)
                {
                    state.State = CircuitStateEnum.Open;
                }

                if (attempt == maxRetries - 1)
                {
                    return new PaymentResult 
                    { 
                        Success = false, 
                        Message = $"Payment processing failed after {maxRetries} attempts: {ex.Message}" 
                    };
                }
            }
        }

        return new PaymentResult 
        { 
            Success = false, 
            Message = "Payment processing failed. Please try again." 
        };
    }
}
