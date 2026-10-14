using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

// Q6: Action inside an existing controller with an injected DbContext named _db.
// Users has Mobile (normalized and UNIQUE) and IsSubscribed.
// Authentication/authorization must confirm this mobile belongs to the caller.
// Assumes there is no external billing operation in this action.

[HttpPost("subscribe")]
public async Task<IActionResult> Subscribe(string mobile, CancellationToken ct)
{
    if (string.IsNullOrWhiteSpace(mobile))
        return BadRequest("Mobile is required.");

    // Atomic compare-and-set. Only one concurrent request can change false to true.
    var changed = await _db.Users
        .Where(u => u.Mobile == mobile && !u.IsSubscribed)
        .ExecuteUpdateAsync(update => update.SetProperty(u => u.IsSubscribed, true), ct);

    if (changed == 1)
        return Ok(new { IsSubscribed = true });

    if (!await _db.Users.AnyAsync(u => u.Mobile == mobile, ct))
        return NotFound("User not found.");

    // Already subscribed is an idempotent success, not a second subscription.
    return Ok(new { IsSubscribed = true });
}
