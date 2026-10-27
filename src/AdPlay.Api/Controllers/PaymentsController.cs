using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using AdPlay.Api.Models;
using AdPlay.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace AdPlay.Api.Controllers;

[ApiController]
[Route("api/payments")]
public class PaymentsController : ControllerBase
{
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    private readonly PaymentService _service;
    private readonly IConfiguration _config;

    public PaymentsController(PaymentService service, IConfiguration config)
    {
        _service = service;
        _config = config;
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CreatePaymentRequest request,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey) || idempotencyKey.Length > 100)
            return BadRequest(new { error = "missing_idempotency_key", message = "Idempotency-Key header is required (max 100 chars)" });

        var result = await _service.CreateAsync(idempotencyKey, request, ct);

        return result.Kind switch
        {
            CreateKind.KeyMismatch => UnprocessableEntity(new { error = "key_reused", message = "This Idempotency-Key was already used with a different request" }),
            CreateKind.OrderBusy => Conflict(new { error = "order_busy", message = "This order already has an active payment" }),
            _ => ToResponse(result.Payment!)
        };
    }

    // The same mapping is used for new requests and replays,
    // so a retry gets the same answer as the original call.
    private IActionResult ToResponse(Payment p)
    {
        var body = new PaymentResponse(p.Id, p.Status.ToString(), p.ProviderRef);

        return p.Status switch
        {
            PaymentStatus.Succeeded => Ok(body),
            PaymentStatus.Failed => StatusCode(StatusCodes.Status402PaymentRequired,
                new { error = "payment_failed", message = p.FailureReason, paymentId = p.Id }),
            PaymentStatus.Unknown => Accepted(body),   // 202: result will arrive later
            _ => Conflict(new { error = "in_progress", message = "Payment is still being processed, retry shortly", paymentId = p.Id })
        };
    }

    [HttpPost("webhook")]
    [RequestSizeLimit(10_000)]
    public async Task<IActionResult> Webhook(CancellationToken ct)
    {
        // read the raw body, the signature is calculated over the exact bytes
        using var reader = new StreamReader(Request.Body);
        var body = await reader.ReadToEndAsync(ct);

        var signature = Request.Headers["X-Signature"].ToString();
        if (!IsValidSignature(body, signature))
            return Unauthorized();

        WebhookPayload? evt;
        try { evt = JsonSerializer.Deserialize<WebhookPayload>(body, JsonOpts); }
        catch (JsonException) { return BadRequest(); }

        if (evt is null || string.IsNullOrWhiteSpace(evt.EventId) || string.IsNullOrWhiteSpace(evt.Status))
            return BadRequest();

        await _service.HandleWebhookAsync(evt, ct);
        return Ok();   // 200 also for duplicates, so the provider stops retrying
    }

    private bool IsValidSignature(string body, string signature)
    {
        var secret = _config["Payments:WebhookSecret"] ?? "";
        if (secret == "" || signature == "") return false;

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var expected = Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(body))).ToLowerInvariant();

        // constant-time compare
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(expected),
            Encoding.UTF8.GetBytes(signature.Trim().ToLowerInvariant()));
    }
}
