using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDayApi.Auth;
using RaceDayApi.Data;
using RaceDayApi.Dtos;
using RaceDayApi.Models;

namespace RaceDayApi.Controllers;

[ApiController]
[Route("api")]
public class ResultsController : ControllerBase
{
    private readonly RaceDayContext _db;

    public ResultsController(RaceDayContext db)
    {
        _db = db;
    }

    /// <summary>Captures a finish time and position for an enrolment. Organiser only, and only for their own events.</summary>
    /// <param name="id">The enrolment's ID.</param>
    /// <param name="request">finishTime (e.g. "00:42:15") and an optional position.</param>
    /// <returns>201 with the result, 403 if not the event's owner, 404 if the enrolment does not exist, 409 if the enrolment is cancelled or already has a result.</returns>
    [HttpPost("enrolments/{id}/results")]
    [SessionAuthorize("Organiser")]
    [ProducesResponseType(typeof(ResultResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Capture(int id, ResultRequest request)
    {
        var enrolment = await _db.Enrolments.FindAsync(id);
        if (enrolment == null)
            return NotFound(new { message = "Enrolment not found." });

        // Find the event this enrolment belongs to, so we can check the organiser owns it.
        var ownerId = await (from c in _db.Categories
                             join ev in _db.Events on c.EventID equals ev.EventID
                             where c.CategoryID == enrolment.CategoryID
                             select ev.OrganiserID).FirstAsync();

        var organiserId = HttpContext.Session.GetInt32("UserId")!.Value;
        if (ownerId != organiserId)
            return StatusCode(403, new { message = "You can only capture results for your own events." });

        if (enrolment.Status != "Confirmed")
            return Conflict(new { message = "Results cannot be captured for a cancelled enrolment." });

        if (await _db.Results.AnyAsync(r => r.EnrolmentID == id))
            return Conflict(new { message = "A result has already been captured for this enrolment." });

        var result = new Result
        {
            EnrolmentID = id,
            FinishTime = request.FinishTime,
            Position = request.Position,
            CapturedBy = organiserId
        };
        _db.Results.Add(result);
        await _db.SaveChangesAsync();

        var response = await BaseQuery().FirstAsync(r => r.ResultId == result.ResultID);
        return Created($"/api/enrolments/{id}/results", response);
    }

    /// <summary>Retrieves the logged-in Participant's own result history. Participant only.</summary>
    /// <param name="id">Must match the logged-in participant's ID.</param>
    /// <returns>200 with an array of results, or 403 if the ID is not the caller's own.</returns>
    [HttpGet("participants/{id}/results")]
    [SessionAuthorize("Participant")]
    [ProducesResponseType(typeof(List<ResultResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetForParticipant(int id)
    {
        var participantId = HttpContext.Session.GetInt32("UserId")!.Value;
        if (id != participantId)
            return StatusCode(403, new { message = "You can only view your own results." });

        var list = await BaseQuery()
            .Where(r => r.ParticipantId == id)
            .OrderBy(r => r.EventName)
            .ToListAsync();
        return Ok(list);
    }

    /// <summary>Retrieves all results for an event, ordered by position then finish time. Public.</summary>
    /// <param name="id">The event's ID.</param>
    /// <returns>200 with an array of results, or 404 if the event does not exist.</returns>
    [HttpGet("events/{id}/results")]
    [ProducesResponseType(typeof(List<ResultResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetForEvent(int id)
    {
        if (!await _db.Events.AnyAsync(e => e.EventID == id))
            return NotFound(new { message = "Event not found." });

        var list = await BaseQuery()
            .Where(r => r.EventId == id)
            .OrderBy(r => r.Position == null)
            .ThenBy(r => r.Position)
            .ThenBy(r => r.FinishTime)
            .ToListAsync();
        return Ok(list);
    }

    // Joins Result + Enrolment + Category + Event + Participant so the response shows names, not just IDs.
    private IQueryable<ResultResponse> BaseQuery() =>
        from r in _db.Results
        join en in _db.Enrolments on r.EnrolmentID equals en.EnrolmentID
        join c in _db.Categories on en.CategoryID equals c.CategoryID
        join ev in _db.Events on c.EventID equals ev.EventID
        join p in _db.Participants on en.ParticipantID equals p.ParticipantID
        select new ResultResponse
        {
            ResultId = r.ResultID,
            EnrolmentId = en.EnrolmentID,
            ParticipantId = p.ParticipantID,
            ParticipantName = p.FirstName + " " + p.LastName,
            EventId = ev.EventID,
            EventName = ev.EventName,
            CategoryId = c.CategoryID,
            CategoryName = c.CategoryName,
            FinishTime = r.FinishTime,
            Position = r.Position
        };
}