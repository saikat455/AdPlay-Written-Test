namespace AdPlay.Api.Models;

public enum PaymentStatus
{
    Pending = 0,    // saved, provider not answered yet
    Succeeded = 1,
    Failed = 2,
    Unknown = 3     // provider timed out, waiting for webhook / reconciliation
}

public class Payment
{
    public Guid Id { get; set; }
    public string IdempotencyKey { get; set; } = "";
    public string RequestHash { get; set; } = "";
    public string OrderRef { get; set; } = "";

    // = OrderRef while the payment is alive, NULL once it failed.
    // Unique index on this blocks a 2nd live payment for the same order.
    public string? ActiveOrderRef { get; set; }

    public decimal Amount { get; set; }
    public string Currency { get; set; } = "BDT";
    public PaymentStatus Status { get; set; }
    public string? ProviderRef { get; set; }
    public string? FailureReason { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

// one row per webhook event we already handled
public class WebhookEvent
{
    public int Id { get; set; }
    public string EventId { get; set; } = "";
    public DateTime ReceivedAt { get; set; }
}
