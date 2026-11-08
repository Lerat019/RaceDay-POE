using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDayApi.Auth;
using RaceDayApi.Data;
using RaceDayApi.Dtos;
using RaceDayApi.Models;

namespace RaceDayApi.Controllers;

[ApiController]
[Route("api")]
public class CategoriesController : ControllerBase
{
    private readonly RaceDayContext _db;

    public CategoriesController(RaceDayContext db)
    {
        _db = db;
    }

    /// <summary>Lists all categories for an event. Public.</summary>
    /// <param name="eventId">The event's ID.</param>
    /// <returns>200 with an array of categories, or 404 if the event does not exist.</returns>
    [HttpGet("events/{eventId}/categories")]
    [ProducesResponseType(typeof(List<CategoryResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetForEvent(int eventId)
    {
        if (!await _db.Events.AnyAsync(e => e.EventID == eventId))
            return NotFound(new { message = "Event not found." });

        var categories = await _db.Categories
            .Where(c => c.EventID == eventId)
            .OrderBy(c => c.Distance)
            .ToListAsync();

        return Ok(categories.Select(ToResponse).ToList());
    }

    /// <summary>Adds a category to an event. Organiser only, and only for their own events.</summary>
    /// <param name="eventId">The event's ID.</param>
    /// <param name="request">categoryName, distance, maxParticipants, entryFee.</param>
    /// <returns>201 with the created category, 403 if not the event's owner, 404 if the event does not exist.</returns>
    [HttpPost("events/{eventId}/categories")]
    [SessionAuthorize("Organiser")]
    [ProducesResponseType(typeof(CategoryResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Create(int eventId, CategoryRequest request)
    {
        var ev = await _db.Events.FindAsync(eventId);
        if (ev == null) return NotFound(new { message = "Event not found." });

        var organiserId = HttpContext.Session.GetInt32("UserId")!.Value;
        if (ev.OrganiserID != organiserId)
            return StatusCode(403, new { message = "You can only add categories to your own events." });

        var category = new Category
        {
            EventID = eventId,
            CategoryName = request.CategoryName.Trim(),
            Distance = request.Distance,
            MaxParticipants = request.MaxParticipants,
            EntryFee = request.EntryFee
        };
        _db.Categories.Add(category);
        await _db.SaveChangesAsync();

        return Created($"/api/categories/{category.CategoryID}", ToResponse(category));
    }

    /// <summary>Updates a category. Organiser only, and only for categories on their own events.</summary>
    /// <param name="id">The category's ID.</param>
    /// <param name="request">categoryName, distance, maxParticipants, entryFee.</param>
    /// <returns>200 with the updated category, 403 if not the owner, 404 if not found.</returns>
    [HttpPut("categories/{id}")]
    [SessionAuthorize("Organiser")]
    [ProducesResponseType(typeof(CategoryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, CategoryRequest request)
    {
        var category = await _db.Categories.FindAsync(id);
        if (category == null) return NotFound(new { message = "Category not found." });

        if (!await OwnsEvent(category.EventID))
            return StatusCode(403, new { message = "You can only change categories on your own events." });

        category.CategoryName = request.CategoryName.Trim();
        category.Distance = request.Distance;
        category.MaxParticipants = request.MaxParticipants;
        category.EntryFee = request.EntryFee;
        await _db.SaveChangesAsync();

        return Ok(ToResponse(category));
    }

    /// <summary>Deletes a category. Organiser only. Fails with 409 if participants are enrolled.</summary>
    /// <param name="id">The category's ID.</param>
    /// <returns>204 on success, 403 if not the owner, 404 if not found, 409 if it has enrolments.</returns>
    [HttpDelete("categories/{id}")]
    [SessionAuthorize("Organiser")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(int id)
    {
        var category = await _db.Categories.FindAsync(id);
        if (category == null) return NotFound(new { message = "Category not found." });

        if (!await OwnsEvent(category.EventID))
            return StatusCode(403, new { message = "You can only delete categories on your own events." });

        if (await _db.Enrolments.AnyAsync(e => e.CategoryID == id))
            return Conflict(new { message = "This category has enrolments and cannot be deleted." });

        _db.Categories.Remove(category);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    private async Task<bool> OwnsEvent(int eventId)
    {
        var organiserId = HttpContext.Session.GetInt32("UserId")!.Value;
        return await _db.Events.AnyAsync(e => e.EventID == eventId && e.OrganiserID == organiserId);
    }

    private static CategoryResponse ToResponse(Category c) => new()
    {
        CategoryId = c.CategoryID,
        EventId = c.EventID,
        CategoryName = c.CategoryName,
        Distance = c.Distance,
        MaxParticipants = c.MaxParticipants,
        EntryFee = c.EntryFee
    };
}
