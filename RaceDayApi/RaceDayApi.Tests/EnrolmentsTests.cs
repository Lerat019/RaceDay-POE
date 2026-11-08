using Microsoft.AspNetCore.Mvc;
using RaceDayApi.Controllers;
using RaceDayApi.Data;
using RaceDayApi.Dtos;
using RaceDayApi.Models;
using Xunit;

namespace RaceDayApi.Tests;

public class EnrolmentsTests
{
    // Pretend the given participant is logged in.
    private static EnrolmentsController AsParticipant(RaceDayContext db, int participantId) =>
        new EnrolmentsController(db).WithSession(participantId, "Participant");

    // Pretend the given organiser is logged in.
    private static EnrolmentsController AsOrganiser(RaceDayContext db, int organiserId) =>
        new EnrolmentsController(db).WithSession(organiserId, "Organiser");

    // Puts one event (owned by organiser 1) with one category into the fake database.
    private static Category SeedCategory(RaceDayContext db, int maxParticipants = 100)
    {
        var ev = new Event
        {
            OrganiserID = 1,
            EventName = "Test 10K",
            EventDate = DateTime.Today.AddDays(30),
            Location = "Cape Town",
            EventType = "Run"
        };
        db.Events.Add(ev);
        db.SaveChanges();

        var category = new Category
        {
            EventID = ev.EventID,
            CategoryName = "10km",
            Distance = 10,
            MaxParticipants = maxParticipants,
            EntryFee = 150
        };
        db.Categories.Add(category);
        db.SaveChanges();
        return category;
    }

    private static Participant AddParticipant(RaceDayContext db, string firstName = "Sipho")
    {
        var p = new Participant
        {
            FirstName = firstName,
            LastName = "Tester",
            Email = $"{firstName.ToLower()}@test.com",
            PasswordHash = "x"
        };
        db.Participants.Add(p);
        db.SaveChanges();
        return p;
    }

    private static Enrolment AddEnrolment(RaceDayContext db, int participantId, int categoryId, string status = "Confirmed")
    {
        var e = new Enrolment
        {
            ParticipantID = participantId,
            CategoryID = categoryId,
            EnrolmentDate = DateTime.Now,
            Status = status
        };
        db.Enrolments.Add(e);
        db.SaveChanges();
        return e;
    }

    // ---------- ENROL ----------

    [Fact]
    public async Task Enrol_ValidCategory_Returns201AndRecordsTheLink()
    {
        using var db = TestHelpers.NewDb();
        var category = SeedCategory(db);
        var participant = AddParticipant(db);

        var result = await AsParticipant(db, participant.ParticipantID).Enrol(category.CategoryID);

        var created = Assert.IsType<CreatedResult>(result);
        var response = Assert.IsType<EnrolmentResponse>(created.Value);
        Assert.Equal(participant.ParticipantID, response.ParticipantId);
        Assert.Equal(category.CategoryID, response.CategoryId);
        Assert.Equal("Test 10K", response.EventName);
        Assert.Equal("Confirmed", response.Status);

        var stored = Assert.Single(db.Enrolments);
        Assert.Equal(participant.ParticipantID, stored.ParticipantID);
        Assert.Equal(category.CategoryID, stored.CategoryID);
    }

    [Fact]
    public async Task Enrol_SameCategoryTwice_Returns409AndNoDuplicate()
    {
        using var db = TestHelpers.NewDb();
        var category = SeedCategory(db);
        var participant = AddParticipant(db);
        var controller = AsParticipant(db, participant.ParticipantID);
        await controller.Enrol(category.CategoryID);

        var result = await controller.Enrol(category.CategoryID);

        Assert.IsType<ConflictObjectResult>(result);
        Assert.Single(db.Enrolments);
    }

    [Fact]
    public async Task Enrol_MissingCategory_Returns404()
    {
        using var db = TestHelpers.NewDb();
        var participant = AddParticipant(db);

        var result = await AsParticipant(db, participant.ParticipantID).Enrol(999);

        Assert.IsType<NotFoundObjectResult>(result);
        Assert.Empty(db.Enrolments);
    }

    [Fact]
    public async Task Enrol_FullCategory_Returns409()
    {
        using var db = TestHelpers.NewDb();
        var category = SeedCategory(db, maxParticipants: 1);
        var first = AddParticipant(db, "Anika");
        var second = AddParticipant(db, "Thabo");
        AddEnrolment(db, first.ParticipantID, category.CategoryID);

        var result = await AsParticipant(db, second.ParticipantID).Enrol(category.CategoryID);

        Assert.IsType<ConflictObjectResult>(result);
        Assert.Single(db.Enrolments);
    }

    [Fact]
    public async Task Enrol_CancelledEnrolmentsDoNotCountTowardsCapacity()
    {
        using var db = TestHelpers.NewDb();
        var category = SeedCategory(db, maxParticipants: 1);
        var first = AddParticipant(db, "Anika");
        var second = AddParticipant(db, "Thabo");
        AddEnrolment(db, first.ParticipantID, category.CategoryID, "Cancelled");

        var result = await AsParticipant(db, second.ParticipantID).Enrol(category.CategoryID);

        Assert.IsType<CreatedResult>(result);
    }

    [Fact]
    public async Task Enrol_AfterCancelling_ReactivatesTheSameEnrolment()
    {
        using var db = TestHelpers.NewDb();
        var category = SeedCategory(db);
        var participant = AddParticipant(db);
        AddEnrolment(db, participant.ParticipantID, category.CategoryID, "Cancelled");

        var result = await AsParticipant(db, participant.ParticipantID).Enrol(category.CategoryID);

        Assert.IsType<CreatedResult>(result);
        var stored = Assert.Single(db.Enrolments); // no duplicate row
        Assert.Equal("Confirmed", stored.Status);
    }

    // ---------- VIEW OWN ENROLMENTS ----------

    [Fact]
    public async Task GetForParticipant_Own_ReturnsOnlyTheirEnrolments()
    {
        using var db = TestHelpers.NewDb();
        var category = SeedCategory(db);
        var sipho = AddParticipant(db, "Sipho");
        var anika = AddParticipant(db, "Anika");
        AddEnrolment(db, sipho.ParticipantID, category.CategoryID);
        AddEnrolment(db, anika.ParticipantID, category.CategoryID);

        var result = await AsParticipant(db, sipho.ParticipantID).GetForParticipant(sipho.ParticipantID);

        var ok = Assert.IsType<OkObjectResult>(result);
        var list = Assert.IsType<List<EnrolmentResponse>>(ok.Value);
        var only = Assert.Single(list);
        Assert.Equal(sipho.ParticipantID, only.ParticipantId);
    }

    [Fact]
    public async Task GetForParticipant_SomeoneElsesId_Returns403()
    {
        using var db = TestHelpers.NewDb();
        var sipho = AddParticipant(db, "Sipho");
        var anika = AddParticipant(db, "Anika");

        var result = await AsParticipant(db, sipho.ParticipantID).GetForParticipant(anika.ParticipantID);

        var forbidden = Assert.IsType<ObjectResult>(result);
        Assert.Equal(403, forbidden.StatusCode);
    }

    // ---------- ORGANISER VIEWS EVENT ENROLMENTS ----------

    [Fact]
    public async Task GetForEvent_OwnerSeesAllEnrolments()
    {
        using var db = TestHelpers.NewDb();
        var category = SeedCategory(db);
        var sipho = AddParticipant(db, "Sipho");
        var anika = AddParticipant(db, "Anika");
        AddEnrolment(db, sipho.ParticipantID, category.CategoryID);
        AddEnrolment(db, anika.ParticipantID, category.CategoryID);

        var result = await AsOrganiser(db, 1).GetForEvent(category.EventID);

        var ok = Assert.IsType<OkObjectResult>(result);
        var list = Assert.IsType<List<EnrolmentResponse>>(ok.Value);
        Assert.Equal(2, list.Count);
    }

    [Fact]
    public async Task GetForEvent_AnotherOrganiser_Returns403()
    {
        using var db = TestHelpers.NewDb();
        var category = SeedCategory(db); // owned by organiser 1

        var result = await AsOrganiser(db, 2).GetForEvent(category.EventID);

        var forbidden = Assert.IsType<ObjectResult>(result);
        Assert.Equal(403, forbidden.StatusCode);
    }

    [Fact]
    public async Task GetForEvent_MissingEvent_Returns404()
    {
        using var db = TestHelpers.NewDb();

        var result = await AsOrganiser(db, 1).GetForEvent(999);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    // ---------- CANCEL ----------

    [Fact]
    public async Task Cancel_OwnEnrolment_Returns204AndMarksCancelled()
    {
        using var db = TestHelpers.NewDb();
        var category = SeedCategory(db);
        var participant = AddParticipant(db);
        var enrolment = AddEnrolment(db, participant.ParticipantID, category.CategoryID);

        var result = await AsParticipant(db, participant.ParticipantID).Cancel(enrolment.EnrolmentID);

        Assert.IsType<NoContentResult>(result);
        Assert.Equal("Cancelled", db.Enrolments.Single().Status);
    }

    [Fact]
    public async Task Cancel_SomeoneElsesEnrolment_Returns403AndKeepsItConfirmed()
    {
        using var db = TestHelpers.NewDb();
        var category = SeedCategory(db);
        var sipho = AddParticipant(db, "Sipho");
        var anika = AddParticipant(db, "Anika");
        var enrolment = AddEnrolment(db, sipho.ParticipantID, category.CategoryID);

        var result = await AsParticipant(db, anika.ParticipantID).Cancel(enrolment.EnrolmentID);

        var forbidden = Assert.IsType<ObjectResult>(result);
        Assert.Equal(403, forbidden.StatusCode);
        Assert.Equal("Confirmed", db.Enrolments.Single().Status);
    }

    [Fact]
    public async Task Cancel_MissingEnrolment_Returns404()
    {
        using var db = TestHelpers.NewDb();
        var participant = AddParticipant(db);

        var result = await AsParticipant(db, participant.ParticipantID).Cancel(999);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task Cancel_AlreadyCancelled_Returns409()
    {
        using var db = TestHelpers.NewDb();
        var category = SeedCategory(db);
        var participant = AddParticipant(db);
        var enrolment = AddEnrolment(db, participant.ParticipantID, category.CategoryID, "Cancelled");

        var result = await AsParticipant(db, participant.ParticipantID).Cancel(enrolment.EnrolmentID);

        Assert.IsType<ConflictObjectResult>(result);
    }

    [Fact]
    public async Task Cancel_WhenResultAlreadyCaptured_Returns409()
    {
        using var db = TestHelpers.NewDb();
        var category = SeedCategory(db);
        var participant = AddParticipant(db);
        var enrolment = AddEnrolment(db, participant.ParticipantID, category.CategoryID);
        db.Results.Add(new Result
        {
            EnrolmentID = enrolment.EnrolmentID,
            FinishTime = TimeSpan.FromMinutes(42),
            Position = 1,
            CapturedBy = 1
        });
        db.SaveChanges();

        var result = await AsParticipant(db, participant.ParticipantID).Cancel(enrolment.EnrolmentID);

        Assert.IsType<ConflictObjectResult>(result);
        Assert.Equal("Confirmed", db.Enrolments.Single().Status);
    }
}