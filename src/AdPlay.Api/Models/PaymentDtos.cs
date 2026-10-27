using System.ComponentModel.DataAnnotations;

namespace AdPlay.Api.Models;

public record CreatePaymentRequest(
    [Required, StringLength(100)] string OrderRef,
    [Range(0.01, 1000000)] decimal Amount,
    [Required, StringLength(3, MinimumLength = 3)] string Currency);

public record WebhookPayload(string EventId, Guid PaymentId, string Status, string? ProviderRef);

public record PaymentResponse(Guid PaymentId, string Status, string? ProviderRef);
