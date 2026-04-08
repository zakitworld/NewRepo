using EventHub.Api.Data;
using EventHub.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EventHub.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TicketsController : ControllerBase
{
    private readonly ApiDbContext _db;
    private readonly ILogger<TicketsController> _logger;

    public TicketsController(ApiDbContext db, ILogger<TicketsController> logger)
    {
        _db     = db;
        _logger = logger;
    }

    // ── GET /api/tickets?userId= ──────────────────────────────────────────
    [HttpGet]
    public async Task<IActionResult> GetByUser([FromQuery] string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return BadRequest("userId is required");

        var tickets = await _db.Tickets
            .Where(t => t.UserId == userId)
            .OrderByDescending(t => t.PurchasedAt)
            .ToListAsync();

        return Ok(tickets);
    }

    // ── GET /api/tickets/{id} ─────────────────────────────────────────────
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        var ticket = await _db.Tickets.FindAsync(id);
        return ticket == null ? NotFound() : Ok(ticket);
    }

    // ── POST /api/tickets  (purchase a ticket after Paystack payment) ──────
    [HttpPost]
    public async Task<IActionResult> Purchase([FromBody] PurchaseTicketRequest req)
    {
        // Verify the event exists
        var ev = await _db.Events.FindAsync(req.EventId);
        if (ev == null) return NotFound("Event not found");

        // If paid event, verify Paystack payment
        if (ev.Price > 0)
        {
            var verified = await VerifyPaystackAsync(req.PaymentReference, (double)ev.Price);
            if (!verified)
                return BadRequest("Payment not confirmed by Paystack");

            // Idempotency: don't double-issue if webhook already created the ticket
            var exists = await _db.Tickets
                .AnyAsync(t => t.TransactionId == req.PaymentReference);
            if (exists)
                return Conflict("Ticket already issued for this payment reference");
        }

        var ticket = new ApiTicket
        {
            Id            = Guid.NewGuid().ToString(),
            EventId       = ev.Id,
            EventTitle    = ev.Title,
            UserId        = req.UserId,
            UserName      = req.UserName,
            UserEmail     = req.UserEmail,
            Price         = ev.Price,
            Status        = "Active",
            TransactionId = req.PaymentReference ?? string.Empty,
            PurchasedAt   = DateTime.UtcNow
        };

        _db.Tickets.Add(ticket);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Ticket {TicketId} issued for user {UserId} / event {EventId}",
            ticket.Id, req.UserId, ev.Id);

        return CreatedAtAction(nameof(GetById), new { id = ticket.Id }, ticket);
    }

    // ── POST /api/tickets/{id}/check-in  (Admin / Organizer scan QR) ──────
    [HttpPost("{id}/check-in")]
    public async Task<IActionResult> CheckIn(string id)
    {
        var ticket = await _db.Tickets.FindAsync(id);
        if (ticket == null)          return NotFound("Ticket not found");
        if (ticket.Status != "Active") return BadRequest($"Cannot check-in ticket with status '{ticket.Status}'");
        if (ticket.CheckedInAt != null) return Conflict("Ticket already checked in");

        ticket.CheckedInAt = DateTime.UtcNow;
        ticket.Status      = "CheckedIn";
        await _db.SaveChangesAsync();

        _logger.LogInformation("Ticket {TicketId} checked in at {Time}", ticket.Id, ticket.CheckedInAt);
        return Ok(new { message = "Check-in successful", checkedInAt = ticket.CheckedInAt });
    }

    // ── DELETE /api/tickets/{id}  (Admin only) ────────────────────────────
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(string id)
    {
        var ticket = await _db.Tickets.FindAsync(id);
        if (ticket == null) return NotFound();
        _db.Tickets.Remove(ticket);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // ── Paystack verify helper ─────────────────────────────────────────────
    private async Task<bool> VerifyPaystackAsync(string? reference, double expectedAmountGhs)
    {
        if (string.IsNullOrEmpty(reference)) return false;
        try
        {
            var secretKey = HttpContext.RequestServices
                .GetRequiredService<IConfiguration>()["Paystack:SecretKey"];
            using var http = new HttpClient();
            http.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", secretKey);
            var resp = await http.GetFromJsonAsync<PaystackVerifyResponse>(
                $"https://api.paystack.co/transaction/verify/{reference}");
            if (resp?.Status != true || resp.Data?.Status != "success") return false;
            var paidGhs = resp.Data.Amount / 100.0;
            return paidGhs >= expectedAmountGhs;
        }
        catch { return false; }
    }

    private record PaystackVerifyResponse(bool Status, PaystackVerifyData? Data);
    private record PaystackVerifyData(string Status, int Amount, string Reference);
}

public record PurchaseTicketRequest(
    string  EventId,
    string  UserId,
    string  UserName,
    string  UserEmail,
    string? PaymentReference);
