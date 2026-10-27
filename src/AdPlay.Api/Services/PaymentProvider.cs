namespace AdPlay.Api.Services;

public enum ProviderOutcome { Success, Declined, Unknown }

public record ProviderResult(ProviderOutcome Outcome, string? Reference, string? Reason);

public interface IPaymentProvider
{
    // paymentId is sent as the provider's own idempotency key,
    // so calling again after a timeout can't charge twice
    Task<ProviderResult> ChargeAsync(Guid paymentId, decimal amount, string currency, CancellationToken ct);
}

// Fake provider for testing. A real one would use HttpClient with a timeout.
public class FakePaymentProvider : IPaymentProvider
{
    public async Task<ProviderResult> ChargeAsync(Guid paymentId, decimal amount, string currency, CancellationToken ct)
    {
        await Task.Delay(300, ct);

        if (amount == 999) return new(ProviderOutcome.Declined, null, "Card declined");
        if (amount == 777) return new(ProviderOutcome.Unknown, null, "Provider timeout");

        return new(ProviderOutcome.Success, "PRV-" + paymentId.ToString("N")[..10], null);
    }
}
