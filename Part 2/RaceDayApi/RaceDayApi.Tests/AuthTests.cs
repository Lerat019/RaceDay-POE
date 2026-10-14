using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RaceDayApi.Controllers;
using RaceDayApi.Data;
using RaceDayApi.Dtos;
using Xunit;

namespace RaceDayApi.Tests;

public class AuthTests
{
    private const string Password = "Password123";

    private static AuthController NewController(RaceDayContext db) =>
        new AuthController(db).WithSession();

    private static RegisterRequest NewRequest(string email = "sipho@test.com", string role = "Participant") => new()
    {
        FirstName = "Sipho",
        LastName = "Dlamini",
        Email = email,
        Password = Password,
        Role = role
    };

    // ---------- REGISTER ----------

    [Fact]
    public async Task Register_Participant_Returns201()
    {
        using var db = TestHelpers.NewDb();
        var controller = NewController(db);

        var result = await controller.Register(NewRequest());

        var created = Assert.IsType<CreatedResult>(result);
        var user = Assert.IsType<UserResponse>(created.Value);
        Assert.Equal("Participant", user.Role);
        Assert.Single(db.Participants);
    }

    [Fact]
    public async Task Register_Organiser_Returns201()
    {
        using var db = TestHelpers.NewDb();
        var controller = NewController(db);

        var result = await controller.Register(NewRequest("org@test.com", "Organiser"));

        var created = Assert.IsType<CreatedResult>(result);
        var user = Assert.IsType<UserResponse>(created.Value);
        Assert.Equal("Organiser", user.Role);
        Assert.Single(db.Organisers);
    }

    [Fact]
    public async Task Register_DuplicateEmail_Returns409()
    {
        using var db = TestHelpers.NewDb();
        var controller = NewController(db);
        await controller.Register(NewRequest());

        var result = await controller.Register(NewRequest());

        Assert.IsType<ConflictObjectResult>(result);
        Assert.Single(db.Participants);
    }

    [Fact]
    public async Task Register_InvalidRole_Returns400()
    {
        using var db = TestHelpers.NewDb();
        var controller = NewController(db);

        var result = await controller.Register(NewRequest(role: "Admin"));

        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Empty(db.Participants);
        Assert.Empty(db.Organisers);
    }

    [Fact]
    public async Task Register_PasswordIsHashed_NotStoredAsPlainText()
    {
        using var db = TestHelpers.NewDb();
        var controller = NewController(db);

        await controller.Register(NewRequest());

        var stored = db.Participants.Single().PasswordHash;
        Assert.NotEqual(Password, stored);
        Assert.StartsWith("$2", stored); // BCrypt hashes start with $2
    }

    // ---------- LOGIN ----------

    [Fact]
    public async Task Login_ValidCredentials_Returns200AndStartsSession()
    {
        using var db = TestHelpers.NewDb();
        var controller = NewController(db);
        await controller.Register(NewRequest());

        var result = await controller.Login(new LoginRequest { Email = "sipho@test.com", Password = Password });

        var ok = Assert.IsType<OkObjectResult>(result);
        var user = Assert.IsType<UserResponse>(ok.Value);
        Assert.Equal("Participant", user.Role);
        Assert.Equal(user.Id, controller.HttpContext.Session.GetInt32("UserId"));
        Assert.Equal("Participant", controller.HttpContext.Session.GetString("Role"));
    }

    [Fact]
    public async Task Login_Organiser_StoresOrganiserRoleInSession()
    {
        using var db = TestHelpers.NewDb();
        var controller = NewController(db);
        await controller.Register(NewRequest("org@test.com", "Organiser"));

        await controller.Login(new LoginRequest { Email = "org@test.com", Password = Password });

        Assert.Equal("Organiser", controller.HttpContext.Session.GetString("Role"));
    }

    [Fact]
    public async Task Login_WrongPassword_Returns401()
    {
        using var db = TestHelpers.NewDb();
        var controller = NewController(db);
        await controller.Register(NewRequest());

        var result = await controller.Login(new LoginRequest { Email = "sipho@test.com", Password = "WrongPassword" });

        Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.Null(controller.HttpContext.Session.GetInt32("UserId"));
    }

    [Fact]
    public async Task Login_UnknownEmail_Returns401()
    {
        using var db = TestHelpers.NewDb();
        var controller = NewController(db);

        var result = await controller.Login(new LoginRequest { Email = "nobody@test.com", Password = Password });

        Assert.IsType<UnauthorizedObjectResult>(result);
    }
}