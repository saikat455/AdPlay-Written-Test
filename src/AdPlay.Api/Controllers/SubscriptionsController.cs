using System.ComponentModel.DataAnnotations;
using AdPlay.Api.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AdPlay.Api.Controllers;

public record SubscribeRequest([Required, StringLength(20, MinimumLength = 6)] string Mobile);

[ApiController]
[Route("api/subscriptions")]
public class SubscriptionsController : ControllerBase
{
    private readonly AppDbContext _db;

    public SubscriptionsController(AppDbContext db) => _db = db;

    [HttpPost]
    public async Task<IActionResult> Subscribe(SubscribeRequest request, CancellationToken ct)
    {
        // Single atomic UPDATE. The WHERE IsSubscribed = false check and the write
        // happen in one statement, so only one of 1000 requests can win.
        var updated = await _db.Users
            .Where(u => u.Mobile == request.Mobile && !u.IsSubscribed)
            .ExecuteUpdateAsync(s => s
                .SetProperty(u => u.IsSubscribed, true)
                .SetProperty(u => u.SubscribedAt, DateTime.UtcNow), ct);

        if (updated == 1)
            return Ok(new { message = "Subscribed" });

        // 0 rows: either no such user or already subscribed
        var exists = await _db.Users.AnyAsync(u => u.Mobile == request.Mobile, ct);

        return exists
            ? Conflict(new { message = "Already subscribed" })   // 409
            : NotFound(new { message = "User not found" });      // 404
    }
}
