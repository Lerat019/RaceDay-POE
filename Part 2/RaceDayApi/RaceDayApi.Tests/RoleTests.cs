using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using RaceDayApi.Auth;
using RaceDayApi.Controllers;
using Xunit;

namespace RaceDayApi.Tests;

public class RoleTests
{
    // Builds the context the lock receives, with a pretend visitor attached.
    private static AuthorizationFilterContext NewContext(int? userId = null, string? role = null)
    {
        var session = new FakeSession();
        if (userId != null) session.SetInt32("UserId", userId.Value);
        if (role != null) session.SetString("Role", role);

        var http = new DefaultHttpContext { Session = session };
        var actionContext = new ActionContext(http, new RouteData(), new ActionDescriptor());
        return new AuthorizationFilterContext(actionContext, new List<IFilterMetadata>());
    }

    // ---------- NOT LOGGED IN -> 401 ----------

    [Fact]
    public void LoggedOut_OnOrganiserEndpoint_Returns401()
    {
        var context = NewContext();

        new SessionAuthorizeAttribute("Organiser").OnAuthorization(context);

        Assert.IsType<UnauthorizedObjectResult>(context.Result);
    }

    [Fact]
    public void LoggedOut_OnParticipantEndpoint_Returns401()
    {
        var context = NewContext();

        new SessionAuthorizeAttribute("Participant").OnAuthorization(context);

        Assert.IsType<UnauthorizedObjectResult>(context.Result);
    }

    [Fact]
    public void LoggedOut_OnAnyLoggedInEndpoint_Returns401()
    {
        var context = NewContext();

        new SessionAuthorizeAttribute().OnAuthorization(context);

        Assert.IsType<UnauthorizedObjectResult>(context.Result);
    }

    [Fact]
    public void IncompleteSession_MissingRole_Returns401()
    {
        var context = NewContext(userId: 1, role: null);

        new SessionAuthorizeAttribute("Organiser").OnAuthorization(context);

        Assert.IsType<UnauthorizedObjectResult>(context.Result);
    }

    // ---------- WRONG ROLE -> 403 ----------

    [Fact]
    public void Participant_OnOrganiserEndpoint_Returns403()
    {
        var context = NewContext(1, "Participant");

        new SessionAuthorizeAttribute("Organiser").OnAuthorization(context);

        var result = Assert.IsType<ObjectResult>(context.Result);
        Assert.Equal(403, result.StatusCode);
    }

    [Fact]
    public void Organiser_OnParticipantEndpoint_Returns403()
    {
        var context = NewContext(1, "Organiser");

        new SessionAuthorizeAttribute("Participant").OnAuthorization(context);

        var result = Assert.IsType<ObjectResult>(context.Result);
        Assert.Equal(403, result.StatusCode);
    }

    // ---------- CORRECT ROLE -> allowed ----------

    [Fact]
    public void Organiser_OnOrganiserEndpoint_IsAllowed()
    {
        var context = NewContext(1, "Organiser");

        new SessionAuthorizeAttribute("Organiser").OnAuthorization(context);

        Assert.Null(context.Result); // no result set means the request carries on
    }

    [Fact]
    public void Participant_OnParticipantEndpoint_IsAllowed()
    {
        var context = NewContext(1, "Participant");

        new SessionAuthorizeAttribute("Participant").OnAuthorization(context);

        Assert.Null(context.Result);
    }

    [Theory]
    [InlineData("Organiser")]
    [InlineData("Participant")]
    public void AnyLoggedInRole_OnAnyLoggedInEndpoint_IsAllowed(string role)
    {
        var context = NewContext(1, role);

        new SessionAuthorizeAttribute().OnAuthorization(context);

        Assert.Null(context.Result);
    }

    // ---------- EVERY PROTECTED ENDPOINT HAS THE LOCK ----------

    [Theory]
    [InlineData(typeof(AuthController), "Logout")]
    [InlineData(typeof(EventsController), "Create")]
    [InlineData(typeof(EventsController), "Update")]
    [InlineData(typeof(EventsController), "Delete")]
    [InlineData(typeof(CategoriesController), "Create")]
    [InlineData(typeof(CategoriesController), "Update")]
    [InlineData(typeof(CategoriesController), "Delete")]
    [InlineData(typeof(EnrolmentsController), "Enrol")]
    [InlineData(typeof(EnrolmentsController), "GetForParticipant")]
    [InlineData(typeof(EnrolmentsController), "GetForEvent")]
    [InlineData(typeof(EnrolmentsController), "Cancel")]
    [InlineData(typeof(ResultsController), "Capture")]
    [InlineData(typeof(ResultsController), "GetForParticipant")]
    public void ProtectedEndpoint_HasSessionAuthorizeLock(Type controller, string action)
    {
        var method = controller.GetMethod(action);

        Assert.NotNull(method);
        Assert.NotEmpty(method!.GetCustomAttributes(typeof(SessionAuthorizeAttribute), true));
    }

    [Fact]
    public void UsersController_IsLockedAtClassLevel()
    {
        Assert.NotEmpty(typeof(UsersController).GetCustomAttributes(typeof(SessionAuthorizeAttribute), true));
    }
}
