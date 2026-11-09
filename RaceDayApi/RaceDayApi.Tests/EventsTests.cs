using Microsoft.AspNetCore.Mvc;
using RaceDayApi.Controllers;
using RaceDayApi.Data;
using RaceDayApi.Dtos;
using RaceDayApi.Models;
using Xunit;

namespace RaceDayApi.Tests;

public class EventsTests
{
    // Pretend organiser 1 is logged in, unless a different ID is given.
    private static EventsController NewController(RaceDayContext db, int organiserId = 1) =>
        new EventsController(db, null!).WithSession(organiserId, "Organiser");

    private static EventRequest NewRequest(string name = "Test 10K") => new()
    {
        EventName = name,
        EventDate = DateTime.Today.AddDays(30),
        Location = "Cape Town",
        Description = "Testing",
        EventType = "Run"
    };

    // Puts an event straight into the fake database.
    private static Event Seed(RaceDayContext db, int organiserId = 1, int daysFromToday = 30)
    {
        var ev = new Event
        {
            OrganiserID = organiserId,
            EventName = "Seeded Event",
            EventDate = DateTime.Today.AddDays(daysFromToday),
            Location = "Durban",
            EventType = "Run"
        };
        db.Events.Add(ev);
        db.SaveChanges();
        return ev;
    }

    // ---------- CREATE ----------

    [Fact]
    public async Task Create_AsOrganiser_Returns201AndSavesEvent()
    {
        using var db = TestHelpers.NewDb();
        var controller = NewController(db);

        var result = await controller.Create(NewRequest(), null);

        var created = Assert.IsType<CreatedAtActionResult>(result);
        var response = Assert.IsType<EventResponse>(created.Value);
        Assert.Equal("Test 10K", response.EventName);
        Assert.Single(db.Events);
    }

    [Fact]
    public async Task Create_SetsOrganiserFromSession_NotFromRequest()
    {
        using var db = TestHelpers.NewDb();
        var controller = NewController(db, organiserId: 7);

        await controller.Create(NewRequest(), null);

        Assert.Equal(7, db.Events.Single().OrganiserID);
    }

    // ---------- UPDATE ----------

    [Fact]
    public async Task Update_OwnEvent_Returns200AndChangesEvent()
    {
        using var db = TestHelpers.NewDb();
        var ev = Seed(db, organiserId: 1);
        var controller = NewController(db, organiserId: 1);

        var result = await controller.Update(ev.EventID, NewRequest("Renamed Event"));

        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<EventResponse>(ok.Value);
        Assert.Equal("Renamed Event", response.EventName);
        Assert.Equal("Renamed Event", db.Events.Single().EventName);
    }

    [Fact]
    public async Task Update_AnotherOrganisersEvent_Returns403AndDoesNotChangeIt()
    {
        using var db = TestHelpers.NewDb();
        var ev = Seed(db, organiserId: 1);
        var controller = NewController(db, organiserId: 2); // a different organiser

        var result = await controller.Update(ev.EventID, NewRequest("Hacked"));

        var forbidden = Assert.IsType<ObjectResult>(result);
        Assert.Equal(403, forbidden.StatusCode);
        Assert.Equal("Seeded Event", db.Events.Single().EventName);
    }

    [Fact]
    public async Task Update_MissingEvent_Returns404()
    {
        using var db = TestHelpers.NewDb();
        var controller = NewController(db);

        var result = await controller.Update(999, NewRequest());

        Assert.IsType<NotFoundObjectResult>(result);
    }

    // ---------- DELETE ----------

    [Fact]
    public async Task Delete_OwnEvent_Returns204AndRemovesEvent()
    {
        using var db = TestHelpers.NewDb();
        var ev = Seed(db, organiserId: 1);
        var controller = NewController(db, organiserId: 1);

        var result = await controller.Delete(ev.EventID);

        Assert.IsType<NoContentResult>(result);
        Assert.Empty(db.Events);
    }

    [Fact]
    public async Task Delete_AnotherOrganisersEvent_Returns403AndKeepsEvent()
    {
        using var db = TestHelpers.NewDb();
        var ev = Seed(db, organiserId: 1);
        var controller = NewController(db, organiserId: 2);

        var result = await controller.Delete(ev.EventID);

        var forbidden = Assert.IsType<ObjectResult>(result);
        Assert.Equal(403, forbidden.StatusCode);
        Assert.Single(db.Events);
    }

    [Fact]
    public async Task Delete_EventThatHasCategories_Returns409()
    {
        using var db = TestHelpers.NewDb();
        var ev = Seed(db, organiserId: 1);
        db.Categories.Add(new Category
        {
            EventID = ev.EventID,
            CategoryName = "10km",
            Distance = 10,
            MaxParticipants = 100,
            EntryFee = 150
        });
        db.SaveChanges();
        var controller = NewController(db, organiserId: 1);

        var result = await controller.Delete(ev.EventID);

        Assert.IsType<ConflictObjectResult>(result);
        Assert.Single(db.Events);
    }

    // ---------- VIEW (public) ----------

    [Fact]
    public async Task GetUpcoming_ReturnsOnlyFutureEvents()
    {
        using var db = TestHelpers.NewDb();
        Seed(db, daysFromToday: 10);   // upcoming
        Seed(db, daysFromToday: -10);  // already happened
        var controller = new EventsController(db, null!).WithSession(); // not logged in: this endpoint is public

        var result = await controller.GetUpcoming();

        var ok = Assert.IsType<OkObjectResult>(result);
        var list = Assert.IsType<List<EventResponse>>(ok.Value);
        Assert.Single(list);
    }

    [Fact]
    public async Task GetById_MissingEvent_Returns404()
    {
        using var db = TestHelpers.NewDb();
        var controller = new EventsController(db, null!).WithSession();

        var result = await controller.GetById(999);

        Assert.IsType<NotFoundObjectResult>(result);
    }
}
