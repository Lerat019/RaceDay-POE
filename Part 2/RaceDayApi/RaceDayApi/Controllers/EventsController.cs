using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDayApi.Auth;
using RaceDayApi.Data;
using RaceDayApi.Dtos;
using RaceDayApi.Models;

namespace RaceDayApi.Controllers;

[ApiController]
[Route("api/events")]
public class EventsController : ControllerBase
{
    private readonly RaceDayContext _db;

    public EventsController(RaceDayContext db)
    {
        _db = db;
    }

    /// <summary>Lists all upcoming events (public).</summary>
    /// <response code="200">Array of upcoming events, soonest first.</response>
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

    /// <summary>Retrieves one event by ID (public).</summary>
    /// <response code="200">The event.</response>
    /// <response code="404">No event with that ID.</response>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(EventResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id)
    {
        var ev = await _db.Events.FindAsync(id);
        if (ev == null) return NotFound(new { message = "Event not found." });
        return Ok(ToResponse(ev));
    }

    /// <summary>Creates a new event (Organiser only).</summary>
    /// <remarks>Body: eventName, eventDate, location, optional description. The event is linked to the logged-in organiser.</remarks>
    /// <response code="201">Event created.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Logged in as a Participant.</response>
    [HttpPost]
    [SessionAuthorize("Organiser")]
    [ProducesResponseType(typeof(EventResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Create(EventRequest request)
    {
        var organiserId = HttpContext.Session.GetInt32("UserId")!.Value;

        var ev = new Event
        {
            OrganiserID = organiserId,
            EventName = request.EventName.Trim(),
            EventDate = request.EventDate.Date,
            Location = request.Location.Trim(),
            Description = request.Description
        };
        _db.Events.Add(ev);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = ev.EventID }, ToResponse(ev));
    }

    /// <summary>Updates an event (Organiser who owns it).</summary>
    /// <remarks>Body: eventName, eventDate, location, optional description.</remarks>
    /// <response code="200">Updated event.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not an Organiser, or not the owner of this event.</response>
    /// <response code="404">No event with that ID.</response>
    [HttpPut("{id}")]
    [SessionAuthorize("Organiser")]
    [ProducesResponseType(typeof(EventResponse), StatusCodes.Status200OK)]
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
        ev.Description = request.Description;
        await _db.SaveChangesAsync();

        return Ok(ToResponse(ev));
    }

    /// <summary>Deletes an event (Organiser who owns it).</summary>
    /// <response code="204">Event deleted.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not an Organiser, or not the owner of this event.</response>
    /// <response code="404">No event with that ID.</response>
    /// <response code="409">Event still has categories; delete those first.</response>
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
        Description = e.Description
    };
}