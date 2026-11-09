using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using RaceDayApi.Auth;
using RaceDayApi.Data;
using RaceDayApi.Dtos;
using RaceDayApi.Models;
using RaceDayApi.Services;

namespace RaceDayApi.Controllers;

[ApiController]
[Route("api/events")]
public class EventsController : ControllerBase
{
    private readonly RaceDayContext _db;
    private readonly IBlobService _blobService;

    public EventsController(RaceDayContext db, IBlobService blobService)
    {
        _db = db;
        _blobService = blobService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(List<EventResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUpcoming()
    {
        var today = DateTime.Today;
        var events = await _db.Events
            .Where(e => e.EventDate >= today)
            .OrderBy(e => e.EventDate)
            .ToListAsync();
        return Ok(events.Select(ToResponse).ToList());
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(EventResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id)
    {
        var ev = await _db.Events.FindAsync(id);
        if (ev == null) return NotFound(new { message = "Event not found." });
        return Ok(ToResponse(ev));
    }

    [HttpPost]
    [SessionAuthorize("Organiser")]
    [ProducesResponseType(typeof(EventResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Create([FromForm] EventRequest request, IFormFile? BannerFile)
    {
        var organiserId = HttpContext.Session.GetInt32("UserId")!.Value;

        string? imageUrl = null;
        if (BannerFile != null && BannerFile.Length > 0)
        {
            imageUrl = await _blobService.UploadFileAsync(BannerFile);
        }

        var ev = new Event
        {
            OrganiserID = organiserId,
            EventName = request.EventName.Trim(),
            EventDate = request.EventDate.Date,
            Location = request.Location.Trim(),
            EventType = request.EventType,
            Description = request.Description,
            BannerImageUrl = imageUrl
        };

        _db.Events.Add(ev);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = ev.EventID }, ToResponse(ev));
    }

    [HttpPut("{id}")]
    [SessionAuthorize("Organiser")]
    [ProducesResponseType(typeof(EventResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, EventRequest request)
    {
        var ev = await _db.Events.FindAsync(id);
        if (ev == null) return NotFound(new { message = "Event not found." });

        var organiserId = HttpContext.Session.GetInt32("UserId")!.Value;
        if (ev.OrganiserID != organiserId)
            return StatusCode(403, new { message = "You can only change your own events." });

        ev.EventName = request.EventName.Trim();
        ev.EventDate = request.EventDate.Date;
        ev.Location = request.Location.Trim();
        ev.EventType = request.EventType;
        ev.Description = request.Description;

        await _db.SaveChangesAsync();

        return Ok(ToResponse(ev));
    }

    [HttpDelete("{id}")]
    [SessionAuthorize("Organiser")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(int id)
    {
        var ev = await _db.Events.FindAsync(id);
        if (ev == null) return NotFound(new { message = "Event not found." });

        var organiserId = HttpContext.Session.GetInt32("UserId")!.Value;
        if (ev.OrganiserID != organiserId)
            return StatusCode(403, new { message = "You can only delete your own events." });

        if (await _db.Categories.AnyAsync(c => c.EventID == id))
            return Conflict(new { message = "Delete this event's categories first." });

        _db.Events.Remove(ev);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    private static EventResponse ToResponse(Event e) => new()
    {
        EventId = e.EventID,
        OrganiserId = e.OrganiserID,
        EventName = e.EventName,
        EventDate = e.EventDate,
        Location = e.Location,
        EventType = e.EventType,
        Description = e.Description,
        BannerImageUrl = e.BannerImageUrl
    };
}