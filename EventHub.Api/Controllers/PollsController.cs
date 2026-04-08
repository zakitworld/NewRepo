using EventHub.Api.Data;
using EventHub.Api.Hubs;
using EventHub.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EventHub.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PollsController : ControllerBase
{
    private readonly ApiDbContext _db;
    private readonly IVoteNotifier _notifier;

    public PollsController(ApiDbContext db, IVoteNotifier notifier)
    {
        _db       = db;
        _notifier = notifier;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] bool activeOnly = false)
    {
        var query = _db.Polls.Include(p => p.Options).AsQueryable();
        if (activeOnly)
        {
            var now = DateTime.UtcNow;
            query = query.Where(p => p.IsActive && p.StartDate <= now && p.EndDate >= now);
        }
        return Ok(await query.OrderByDescending(p => p.CreatedAt).ToListAsync());
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        var poll = await _db.Polls.Include(p => p.Options).FirstOrDefaultAsync(p => p.Id == id);
        return poll == null ? NotFound() : Ok(poll);
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Create([FromBody] ApiPoll poll)
    {
        poll.Id        = Guid.NewGuid().ToString();
        poll.CreatedAt = DateTime.UtcNow;
        foreach (var opt in poll.Options)
        {
            opt.Id     = Guid.NewGuid().ToString();
            opt.PollId = poll.Id;
        }
        _db.Polls.Add(poll);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = poll.Id }, poll);
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(string id)
    {
        var poll = await _db.Polls.FindAsync(id);
        if (poll == null) return NotFound();
        _db.Polls.Remove(poll);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // ── Voting ──────────────────────────────────────────────────────────────

    /// <summary>Cast a free vote (no payment required).</summary>
    [HttpPost("{pollId}/vote")]
    [Authorize]
    public async Task<IActionResult> CastFreeVote(string pollId, [FromBody] CastVoteRequest req)
    {
        var poll = await _db.Polls.Include(p => p.Options).FirstOrDefaultAsync(p => p.Id == pollId);
        if (poll == null) return NotFound("Poll not found");
        if (poll.IsPaidVoting) return BadRequest("Use /paid-vote for paid polls");
        if (!poll.IsActive)    return BadRequest("Poll is not active");

        var alreadyVoted = await _db.Votes.AnyAsync(v => v.PollId == pollId && v.UserId == req.UserId);
        if (alreadyVoted) return Conflict("Already voted");

        var option = poll.Options.FirstOrDefault(o => o.Id == req.OptionId);
        if (option == null) return NotFound("Option not found");

        _db.Votes.Add(new ApiVote { PollId = pollId, OptionId = req.OptionId, UserId = req.UserId });
        option.VoteCount++;
        poll.TotalVotes++;
        await _db.SaveChangesAsync();

        await _notifier.BroadcastVoteUpdateAsync(pollId, req.OptionId, option.VoteCount, poll.TotalVotes);
        return Ok(new { message = "Vote recorded", totalVotes = poll.TotalVotes });
    }

    /// <summary>Cast paid votes after Paystack payment is verified server-side.</summary>
    [HttpPost("{pollId}/paid-vote")]
    [Authorize]
    public async Task<IActionResult> CastPaidVote(string pollId, [FromBody] CastPaidVoteRequest req)
    {
        var poll = await _db.Polls.Include(p => p.Options).FirstOrDefaultAsync(p => p.Id == pollId);
        if (poll == null)         return NotFound("Poll not found");
        if (!poll.IsPaidVoting)   return BadRequest("Poll does not support paid voting");
        if (!poll.IsActive)        return BadRequest("Poll is not active");

        // Verify Paystack payment server-side (prevents client spoofing)
        var expected = (double)(req.VoteCount * poll.VotePriceGhs);
        var verified = await VerifyPaystackAsync(req.PaymentReference, expected);
        if (!verified) return BadRequest("Payment not confirmed by Paystack");

        var option = poll.Options.FirstOrDefault(o => o.Id == req.OptionId);
        if (option == null) return NotFound("Option not found");

        // Check max votes constraint
        if (poll.MaxVotesPerUser > 0)
        {
            var cast = await _db.Votes.CountAsync(v => v.PollId == pollId && v.UserId == req.UserId);
            if (cast + req.VoteCount > poll.MaxVotesPerUser)
                return BadRequest($"Exceeds max of {poll.MaxVotesPerUser} votes per user");
        }

        for (int i = 0; i < req.VoteCount; i++)
        {
            _db.Votes.Add(new ApiVote
            {
                PollId     = pollId,
                OptionId   = req.OptionId,
                UserId     = req.UserId,
                PaymentRef = req.PaymentReference
            });
            option.VoteCount++;
            poll.TotalVotes++;
        }

        await _db.SaveChangesAsync();
        await _notifier.BroadcastVoteUpdateAsync(pollId, req.OptionId, option.VoteCount, poll.TotalVotes);
        return Ok(new { message = $"{req.VoteCount} vote(s) recorded", totalVotes = poll.TotalVotes });
    }

    private async Task<bool> VerifyPaystackAsync(string reference, double expectedAmountGhs)
    {
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
            // Paystack amounts are in pesewas (1/100 GHS)
            var paidGhs = resp.Data.Amount / 100.0;
            return paidGhs >= expectedAmountGhs;
        }
        catch { return false; }
    }

    private record PaystackVerifyResponse(bool Status, PaystackVerifyData? Data);
    private record PaystackVerifyData(string Status, int Amount, string Reference);
}

public record CastVoteRequest(string UserId, string OptionId);
public record CastPaidVoteRequest(string UserId, string OptionId, int VoteCount, string PaymentReference);
