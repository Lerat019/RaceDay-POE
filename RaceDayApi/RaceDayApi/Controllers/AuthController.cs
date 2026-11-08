using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDayApi.Auth;
using RaceDayApi.Data;
using RaceDayApi.Dtos;
using RaceDayApi.Models;

namespace RaceDayApi.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly RaceDayContext _db;

    public AuthController(RaceDayContext db)
    {
        _db = db;
    }

    /// <summary>Registers a new Participant or Organiser account.</summary>
    /// <remarks>Body: firstName, lastName, email, password (min 8 characters), role ("Organiser" or "Participant"), optional phoneNumber and dateOfBirth.</remarks>
    /// <response code="201">Account created. Returns the user record.</response>
    /// <response code="400">Invalid input or unknown role.</response>
    /// <response code="409">Email already registered.</response>
    [HttpPost("register")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register(RegisterRequest request)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        // Email must be unique across BOTH tables so login finds one account
        var taken = await _db.Organisers.AnyAsync(o => o.Email == email)
                    || await _db.Participants.AnyAsync(p => p.Email == email);
        if (taken)
            return Conflict(new { message = "Email already registered." });

        var hash = BCrypt.Net.BCrypt.HashPassword(request.Password);

        if (string.Equals(request.Role, "Organiser", StringComparison.OrdinalIgnoreCase))
        {
            var organiser = new Organiser
            {
                FirstName = request.FirstName.Trim(),
                LastName = request.LastName.Trim(),
                Email = email,
                PasswordHash = hash,
                PhoneNumber = request.PhoneNumber
            };
            _db.Organisers.Add(organiser);
            await _db.SaveChangesAsync();
            return Created($"/api/users/{organiser.OrganiserID}", new UserResponse
            {
                Id = organiser.OrganiserID,
                FirstName = organiser.FirstName,
                LastName = organiser.LastName,
                Email = organiser.Email,
                Role = "Organiser",
                PhoneNumber = organiser.PhoneNumber
            });
        }

        if (string.Equals(request.Role, "Participant", StringComparison.OrdinalIgnoreCase))
        {
            var participant = new Participant
            {
                FirstName = request.FirstName.Trim(),
                LastName = request.LastName.Trim(),
                Email = email,
                PasswordHash = hash,
                PhoneNumber = request.PhoneNumber,
                DateOfBirth = request.DateOfBirth
            };
            _db.Participants.Add(participant);
            await _db.SaveChangesAsync();
            return Created($"/api/users/{participant.ParticipantID}", new UserResponse
            {
                Id = participant.ParticipantID,
                FirstName = participant.FirstName,
                LastName = participant.LastName,
                Email = participant.Email,
                Role = "Participant",
                PhoneNumber = participant.PhoneNumber
            });
        }

        return BadRequest(new { message = "Role must be 'Organiser' or 'Participant'." });
    }

    /// <summary>Logs a user in and starts a server-side session.</summary>
    /// <remarks>Body: email and password. The session stores the user's ID and role for later requests.</remarks>
    /// <response code="200">Logged in. Returns the user record.</response>
    /// <response code="401">Invalid credentials.</response>
    [HttpPost("login")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        var organiser = await _db.Organisers.FirstOrDefaultAsync(o => o.Email == email);
        if (organiser != null && BCrypt.Net.BCrypt.Verify(request.Password, organiser.PasswordHash))
        {
            HttpContext.Session.Clear();
            HttpContext.Session.SetInt32("UserId", organiser.OrganiserID);
            HttpContext.Session.SetString("Role", "Organiser");
            return Ok(new UserResponse
            {
                Id = organiser.OrganiserID,
                FirstName = organiser.FirstName,
                LastName = organiser.LastName,
                Email = organiser.Email,
                Role = "Organiser",
                PhoneNumber = organiser.PhoneNumber
            });
        }

        var participant = await _db.Participants.FirstOrDefaultAsync(p => p.Email == email);
        if (participant != null && BCrypt.Net.BCrypt.Verify(request.Password, participant.PasswordHash))
        {
            HttpContext.Session.Clear();
            HttpContext.Session.SetInt32("UserId", participant.ParticipantID);
            HttpContext.Session.SetString("Role", "Participant");
            return Ok(new UserResponse
            {
                Id = participant.ParticipantID,
                FirstName = participant.FirstName,
                LastName = participant.LastName,
                Email = participant.Email,
                Role = "Participant",
                PhoneNumber = participant.PhoneNumber
            });
        }

        return Unauthorized(new { message = "Invalid credentials." });
    }

    /// <summary>Ends the current session.</summary>
    /// <response code="204">Logged out.</response>
    /// <response code="401">Not logged in.</response>
    [HttpPost("logout")]
    [SessionAuthorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        return NoContent();
    }
}
