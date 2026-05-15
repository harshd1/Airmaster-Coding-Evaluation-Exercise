namespace AirmasterOrderApi.Models;

public class PaymentRequest
{
    public Guid OrderId { get; set; }
    public string PaymentProvider { get; set; } = "Stripe";
    public string Token { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "USD";
}

public class PaymentResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? TransactionId { get; set; }
}
