using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDayApi.Data;

namespace RaceDayApi.Tests;

// A pretend session that lives in memory, so tests can "log in" without a browser.
public class FakeSession : ISession
{
    private readonly Dictionary<string, byte[]> _store = new();

    public bool IsAvailable => true;
    public string Id => "test-session";
    public IEnumerable<string> Keys => _store.Keys;

    public void Clear() => _store.Clear();
    public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task LoadAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    public void Remove(string key) => _store.Remove(key);
    public void Set(string key, byte[] value) => _store[key] = value;
    public bool TryGetValue(string key, out byte[] value) => _store.TryGetValue(key, out value!);
}

public static class TestHelpers
{
    // A brand-new empty fake database for every test, so tests never affect each other.
    public static RaceDayContext NewDb()
    {
        var options = new DbContextOptionsBuilder<RaceDayContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new RaceDayContext(options);
    }

    // Gives a controller a fake session. Leave userId and role empty to act as "not logged in".
    public static T WithSession<T>(this T controller, int? userId = null, string? role = null)
        where T : ControllerBase
    {
        var session = new FakeSession();
        if (userId != null) session.SetInt32("UserId", userId.Value);
        if (role != null) session.SetString("Role", role);

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { Session = session }
        };
        return controller;
    }
}