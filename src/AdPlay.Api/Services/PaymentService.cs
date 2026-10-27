using System.Security.Cryptography;
using System.Text;
using AdPlay.Api.Data;
using AdPlay.Api.Models;
using Microsoft.EntityFrameworkCore;
using MySqlConnector;

namespace AdPlay.Api.Services;

public enum CreateKind { Processed, Replayed, KeyMismatch, OrderBusy }

public record CreateResult(CreateKind Kind, Payment? Payment);

public class PaymentService
{
    private readonly AppDbContext _db;
    private readonly IPaymentProvider _provider;
    private readonly ILogger<PaymentService> _log;

    public PaymentService(AppDbContext db, IPaymentProvider provider, ILogger<PaymentService> log)
    {
        _db = db;
        _provider = provider;
        _log = log;
    }

    public async Task<CreateResult> CreateAsync(string key, CreatePaymentRequest req, CancellationToken ct)
    {
        var hash = Hash(req);
        var now = DateTime.UtcNow;

        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            IdempotencyKey = key,
            RequestHash = hash,
            OrderRef = req.OrderRef,
            ActiveOrderRef = req.OrderRef,
            Amount = req.Amount,
            Currency = req.Currency.ToUpperInvariant(),
            Status = PaymentStatus.Pending,
            CreatedAt = now,
            UpdatedAt = now
        };

        // Step 1: save a Pending row first (short transaction, no provider call inside).
        // The unique indexes decide who wins when requests arrive together.
        _db.Payments.Add(payment);
        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (IsDuplicate(ex))
        {
            _db.Entry(payment).State = EntityState.Detached;

            var existing = await _db.Payments.AsNoTracking()
                .FirstOrDefaultAsync(p => p.IdempotencyKey == key, ct);

            // no row with this key -> the clash was the order (another live payment)
            if (existing is null) return new(CreateKind.OrderBusy, null);
            if (existing.RequestHash != hash) return new(CreateKind.KeyMismatch, null);

            return new(CreateKind.Replayed, existing);
        }

        // Step 2: call the provider outside any DB transaction
        ProviderResult result;
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(10));
            result = await _provider.ChargeAsync(payment.Id, payment.Amount, payment.Currency, cts.Token);
        }
        catch (Exception ex) when (ex is OperationCanceledException or HttpRequestException)
        {
            // timeout: we can't tell if the customer was charged, so don't mark it failed
            _log.LogWarning(ex, "Provider call uncertain for payment {PaymentId}", payment.Id);
            result = new(ProviderOutcome.Unknown, null, "Provider did not respond");
        }

        // Step 3: save the final state (CancellationToken.None so a dropped client can't lose it)
        var next = result.Outcome switch
        {
            ProviderOutcome.Success => PaymentStatus.Succeeded,
            ProviderOutcome.Declined => PaymentStatus.Failed,
            _ => PaymentStatus.Unknown
        };

        await MoveAsync(payment.Id, new[] { PaymentStatus.Pending }, next,
            result.Reference, result.Reason, CancellationToken.None);

        // re-read: a webhook may have updated it before us
        var saved = await _db.Payments.AsNoTracking().FirstAsync(p => p.Id == payment.Id, ct);
        return new(CreateKind.Processed, saved);
    }

    public async Task HandleWebhookAsync(WebhookPayload evt, CancellationToken ct)
    {
        // event row + status change in one transaction
        await using var tx = await _db.Database.BeginTransactionAsync(ct);

        _db.WebhookEvents.Add(new WebhookEvent { EventId = evt.EventId, ReceivedAt = DateTime.UtcNow });
        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (IsDuplicate(ex))
        {
            _log.LogInformation("Duplicate webhook {EventId} ignored", evt.EventId);
            return;
        }

        var to = evt.Status.Equals("succeeded", StringComparison.OrdinalIgnoreCase)
            ? PaymentStatus.Succeeded
            : PaymentStatus.Failed;

        var moved = await MoveAsync(evt.PaymentId, new[] { PaymentStatus.Pending, PaymentStatus.Unknown },
            to, evt.ProviderRef, to == PaymentStatus.Failed ? "Failed (webhook)" : null, ct);

        if (!moved)
            _log.LogInformation("Webhook {EventId}: payment {PaymentId} not in a state that can change", evt.EventId, evt.PaymentId);

        await tx.CommitAsync(ct);
    }

    // Only moves the payment if it is currently in one of the "from" states.
    // Succeeded and Failed are final, so late or repeated updates can't overwrite them.
    private async Task<bool> MoveAsync(Guid id, PaymentStatus[] from, PaymentStatus to,
        string? providerRef, string? reason, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var freeOrder = to == PaymentStatus.Failed; // failed -> customer can try the order again

        var rows = await _db.Payments
            .Where(p => p.Id == id && from.Contains(p.Status))
            .ExecuteUpdateAsync(s => s
                .SetProperty(p => p.Status, to)
                .SetProperty(p => p.UpdatedAt, now)
                .SetProperty(p => p.ProviderRef, p => providerRef ?? p.ProviderRef)
                .SetProperty(p => p.FailureReason, p => reason ?? p.FailureReason)
                .SetProperty(p => p.ActiveOrderRef, p => freeOrder ? null : p.ActiveOrderRef), ct);

        return rows == 1;
    }

    private static bool IsDuplicate(DbUpdateException ex) =>
        ex.InnerException is MySqlException { ErrorCode: MySqlErrorCode.DuplicateKeyEntry };

    private static string Hash(CreatePaymentRequest r)
    {
        var raw = $"{r.OrderRef}|{r.Amount:0.00}|{r.Currency.ToUpperInvariant()}";
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(raw)));
    }
}
