using EventHub.Api.Data;
using EventHub.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EventHub.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EventsController : ControllerBase
{
    private readonly ApiDbContext _db;

    public EventsController(ApiDbContext db) => _db = db;

    // ── GET /api/events?category=&search=&activeOnly= ─────────────────────
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string?  category   = null,
        [FromQuery] string?  search     = null,
        [FromQuery] bool     activeOnly = false)
    {
        var query = _db.Events.AsQueryable();

        if (activeOnly)
            query = query.Where(e => e.IsActive);

        if (!string.IsNullOrWhiteSpace(category))
            query = query.Where(e => e.Category == category);

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(e =>
                EF.Functions.Like(e.Title,       $"%{search}%") ||
                EF.Functions.Like(e.Description, $"%{search}%") ||
                EF.Functions.Like(e.Location,    $"%{search}%"));

        var events = await query
            .OrderByDescending(e => e.StartDate)
            .ToListAsync();

        return Ok(events);
    }

    // ── GET /api/events/{id} ──────────────────────────────────────────────
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        var ev = await _db.Events.FindAsync(id);
        return ev == null ? NotFound() : Ok(ev);
    }

    // ── POST /api/events  (Organizer or Admin) ────────────────────────────
    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Create([FromBody] ApiEvent ev)
    {
        ev.Id        = Guid.NewGuid().ToString();
        ev.CreatedAt = DateTime.UtcNow;

        _db.Events.Add(ev);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = ev.Id }, ev);
    }

    // ── PUT /api/events/{id} ──────────────────────────────────────────────
    [HttpPut("{id}")]
    [Authorize]
    public async Task<IActionResult> Update(string id, [FromBody] ApiEvent body)
    {
        var ev = await _db.Events.FindAsync(id);
        if (ev == null) return NotFound();

        ev.Title       = body.Title;
        ev.Description = body.Description;
        ev.Location    = body.Location;
        ev.Category    = body.Category;
        ev.ImageUrl    = body.ImageUrl;
        ev.Price       = body.Price;
        ev.StartDate   = body.StartDate;
        ev.EndDate     = body.EndDate;
        ev.IsActive    = body.IsActive;

        await _db.SaveChangesAsync();
        return Ok(ev);
    }

    // ── DELETE /api/events/{id}  (Admin only) ─────────────────────────────
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(string id)
    {
        var ev = await _db.Events.FindAsync(id);
        if (ev == null) return NotFound();
        _db.Events.Remove(ev);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // ── GET /api/events/categories  (distinct list) ───────────────────────
    [HttpGet("categories")]
    public async Task<IActionResult> GetCategories()
    {
        var cats = await _db.Events
            .Select(e => e.Category)
            .Where(c => c != null && c != string.Empty)
            .Distinct()
            .OrderBy(c => c)
            .ToListAsync();
        return Ok(cats);
    }
}
