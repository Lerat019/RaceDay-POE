using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDayApi.Auth;
using RaceDayApi.Data;
using RaceDayApi.Dtos;
using RaceDayApi.Models;

namespace RaceDayApi.Controllers;

[ApiController]
[Route("api")]
public class EnrolmentsController : ControllerBase
{
    private readonly RaceDayContext _db;

    public EnrolmentsController(RaceDayContext db)
    {
        _db = db;
    }

    /// <summary>Enrols the logged-in Participant into a category. Participant only.</summary>
    /// <param name="id">The category's ID. No request body is needed; the participant comes from the session.</param>
    /// <returns>201 with the enrolment, 404 if the category does not exist, 409 if already enrolled or the category is full.</returns>
    [HttpPost("categories/{id}/enrol")]
    [SessionAuthorize("Participant")]
    [ProducesResponseType(typeof(EnrolmentResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Enrol(int id)
    {
        var participantId = HttpContext.Session.GetInt32("UserId")!.Value;

        var category = await _db.Categories.FindAsync(id);
        if (category == null)
            return NotFound(new { message = "Category not found." });

        var existing = await _db.Enrolments
            .FirstOrDefaultAsync(e => e.ParticipantID == participantId && e.CategoryID == id);

        if (existing != null && existing.Status == "Confirmed")
            return Conflict(new { message = "You are already enrolled in this category." });

        var confirmedCount = await _db.Enrolments
            .CountAsync(e => e.CategoryID == id && e.Status == "Confirmed");
        if (confirmedCount >= category.MaxParticipants)
            return Conflict(new { message = "This category is full." });

        Enrolment enrolment;
        if (existing != null)
        {
            // They cancelled earlier, so re-activate that enrolment instead of making a duplicate.
            existing.Status = "Confirmed";
            existing.EnrolmentDate = DateTime.Now;
            enrolment = existing;
        }
        else
        {
            enrolment = new Enrolment
            {
                ParticipantID = participantId,
                CategoryID = id,
                EnrolmentDate = DateTime.Now,
                Status = "Confirmed"
            };
            _db.Enrolments.Add(enrolment);
        }
        await _db.SaveChangesAsync();

        var response = await BaseQuery().FirstAsync(e => e.EnrolmentId == enrolment.EnrolmentID);
        return Created($"/api/enrolments/{enrolment.EnrolmentID}", response);
    }

    /// <summary>Lists the logged-in Participant's own enrolments. Participant only.</summary>
    /// <param name="id">Must match the logged-in participant's ID.</param>
    /// <returns>200 with an array of enrolments, or 403 if the ID is not the caller's own.</returns>
    [HttpGet("participants/{id}/enrolments")]
    [SessionAuthorize("Participant")]
    [ProducesResponseType(typeof(List<EnrolmentResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetForParticipant(int id)
    {
        var participantId = HttpContext.Session.GetInt32("UserId")!.Value;
        if (id != participantId)
            return StatusCode(403, new { message = "You can only view your own enrolments." });

        var list = await BaseQuery()
            .Where(e => e.ParticipantId == id)
            .OrderByDescending(e => e.EnrolmentDate)
            .ToListAsync();
        return Ok(list);
    }

    /// <summary>Lists all enrolments for an event. Organiser only, and only for their own events.</summary>
    /// <param name="id">The event's ID.</param>
    /// <returns>200 with an array of enrolments, 403 if not the event's owner, 404 if the event does not exist.</returns>
    [HttpGet("events/{id}/enrolments")]
    [SessionAuthorize("Organiser")]
    [ProducesResponseType(typeof(List<EnrolmentResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetForEvent(int id)
    {
        var ev = await _db.Events.FindAsync(id);
        if (ev == null)
            return NotFound(new { message = "Event not found." });

        var organiserId = HttpContext.Session.GetInt32("UserId")!.Value;
        if (ev.OrganiserID != organiserId)
            return StatusCode(403, new { message = "You can only view enrolments for your own events." });

        var list = await BaseQuery()
            .Where(e => e.EventId == id)
            .OrderBy(e => e.CategoryName)
            .ThenBy(e => e.ParticipantName)
            .ToListAsync();
        return Ok(list);
    }

    /// <summary>Cancels one of the logged-in Participant's own enrolments. Participant only.</summary>
    /// <param name="id">The enrolment's ID.</param>
    /// <returns>204 on success, 403 if it is not the caller's enrolment, 404 if not found, 409 if already cancelled or a result exists.</returns>
    [HttpDelete("enrolments/{id}")]
    [SessionAuthorize("Participant")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Cancel(int id)
    {
        var enrolment = await _db.Enrolments.FindAsync(id);
        if (enrolment == null)
            return NotFound(new { message = "Enrolment not found." });

        var participantId = HttpContext.Session.GetInt32("UserId")!.Value;
        if (enrolment.ParticipantID != participantId)
            return StatusCode(403, new { message = "You can only cancel your own enrolments." });

        if (enrolment.Status == "Cancelled")
            return Conflict(new { message = "This enrolment is already cancelled." });

        if (await _db.Results.AnyAsync(r => r.EnrolmentID == id))
            return Conflict(new { message = "A result has already been captured, so this enrolment cannot be cancelled." });

        // Soft cancel: keep the row and mark it Cancelled, which your database's Status column supports.
        enrolment.Status = "Cancelled";
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // Joins Enrolment + Category + Event + Participant so the response shows names, not just IDs.
    private IQueryable<EnrolmentResponse> BaseQuery() =>
        from en in _db.Enrolments
        join c in _db.Categories on en.CategoryID equals c.CategoryID
        join ev in _db.Events on c.EventID equals ev.EventID
        join p in _db.Participants on en.ParticipantID equals p.ParticipantID
        select new EnrolmentResponse
        {
            EnrolmentId = en.EnrolmentID,
            ParticipantId = p.ParticipantID,
            ParticipantName = p.FirstName + " " + p.LastName,
            CategoryId = c.CategoryID,
            CategoryName = c.CategoryName,
            EventId = ev.EventID,
            EventName = ev.EventName,
            EnrolmentDate = en.EnrolmentDate,
            Status = en.Status
        };
}