using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDayApi.Auth;
using RaceDayApi.Data;
using RaceDayApi.Dtos;

namespace RaceDayApi.Controllers;

[ApiController]
[Route("api/users")]
[SessionAuthorize]
public class UsersController : ControllerBase
{
    private readonly RaceDayContext _db;

    public UsersController(RaceDayContext db)
    {
        _db = db;
    }

    /// <summary>Retrieves the logged-in user's own profile. Any logged-in role.</summary>
    /// <param name="id">Must match the logged-in user's ID.</param>
    /// <returns>200 with the profile, 403 if the ID is not the caller's own, 404 if not found.</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(int id)
    {
        var (userId, role) = CurrentUser();
        if (id != userId)
            return StatusCode(403, new { message = "You can only view your own profile." });

        if (role == "Organiser")
        {
            var o = await _db.Organisers.FindAsync(id);
            if (o == null) return NotFound(new { message = "User not found." });
            return Ok(ToResponse(o.OrganiserID, o.FirstName, o.LastName, o.Email, "Organiser", o.PhoneNumber));
        }

        var p = await _db.Participants.FindAsync(id);
        if (p == null) return NotFound(new { message = "User not found." });
        return Ok(ToResponse(p.ParticipantID, p.FirstName, p.LastName, p.Email, "Participant", p.PhoneNumber));
    }

    /// <summary>Updates the logged-in user's own profile (first name, last name, phone number).</summary>
    /// <param name="id">Must match the logged-in user's ID.</param>
    /// <param name="request">firstName, lastName, phoneNumber.</param>
    /// <returns>200 with the updated profile, 403 if the ID is not the caller's own, 404 if not found.</returns>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, UpdateProfileRequest request)
    {
        var (userId, role) = CurrentUser();
        if (id != userId)
            return StatusCode(403, new { message = "You can only update your own profile." });

        if (role == "Organiser")
        {
            var o = await _db.Organisers.FindAsync(id);
            if (o == null) return NotFound(new { message = "User not found." });
            o.FirstName = request.FirstName.Trim();
            o.LastName = request.LastName.Trim();
            o.PhoneNumber = request.PhoneNumber;
            await _db.SaveChangesAsync();
            return Ok(ToResponse(o.OrganiserID, o.FirstName, o.LastName, o.Email, "Organiser", o.PhoneNumber));
        }

        var p = await _db.Participants.FindAsync(id);
        if (p == null) return NotFound(new { message = "User not found." });
        p.FirstName = request.FirstName.Trim();
        p.LastName = request.LastName.Trim();
        p.PhoneNumber = request.PhoneNumber;
        await _db.SaveChangesAsync();
        return Ok(ToResponse(p.ParticipantID, p.FirstName, p.LastName, p.Email, "Participant", p.PhoneNumber));
    }

    // Organiser and Participant IDs overlap (both start at 1), so the session role
    // decides which table to read; the ID alone is not enough.
    private (int userId, string role) CurrentUser() =>
        (HttpContext.Session.GetInt32("UserId")!.Value, HttpContext.Session.GetString("Role")!);

    private static UserResponse ToResponse(int id, string first, string last, string email, string role, string? phone) => new()
    {
        Id = id,
        FirstName = first,
        LastName = last,
        Email = email,
        Role = role,
        PhoneNumber = phone
    };
}
