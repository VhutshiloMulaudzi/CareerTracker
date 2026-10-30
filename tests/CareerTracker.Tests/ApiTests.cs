using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CareerTracker.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CareerTracker.Tests;

public sealed class ApiTests
{
    [Fact]
    public async Task Create_get_filter_and_change_status_round_trip()
    {
        await using var factory = new TestApiFactory();
        using var client = factory.CreateClient();
        var created = await client.PostAsJsonAsync("/api/applications", new { company = "Example Ltd", role = "Backend Developer", appliedOn = "2020-01-01" });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var body = await created.Content.ReadFromJsonAsync<JsonElement>();
        var id = body.GetProperty("id").GetInt32();
        Assert.Equal("Applied", body.GetProperty("status").GetString());
        Assert.Equal($"/api/applications/{id}", created.Headers.Location?.OriginalString);
        var fetched = await client.GetAsync($"/api/applications/{id}");
        Assert.Equal(HttpStatusCode.OK, fetched.StatusCode);
        var changed = await client.PatchAsJsonAsync($"/api/applications/{id}/status", new { status = "Interviewing", expectedVersion = 1 });
        Assert.Equal(HttpStatusCode.OK, changed.StatusCode);
        var current = await changed.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(2, current.GetProperty("version").GetInt32());
        var stale = await client.PatchAsJsonAsync($"/api/applications/{id}/status", new { status = "Offer", expectedVersion = 1 });
        Assert.Equal(HttpStatusCode.Conflict, stale.StatusCode);
        var list = await client.GetFromJsonAsync<JsonElement>("/api/applications?status=Interviewing&page=1&pageSize=10");
        Assert.Equal(1, list.GetProperty("totalCount").GetInt32());
        Assert.Single(list.GetProperty("items").EnumerateArray());
    }

    [Fact]
    public async Task Invalid_input_and_missing_records_return_problem_details()
    {
        await using var factory = new TestApiFactory();
        using var client = factory.CreateClient();
        var invalid = await client.PostAsJsonAsync("/api/applications", new { company = " ", role = "Developer", appliedOn = "2020-01-01" });
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
        Assert.Equal("application/problem+json", invalid.Content.Headers.ContentType?.MediaType);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/api/applications/999")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync("/api/applications?page=0")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/applications", new { company = "Example", role = "Developer", appliedOn = "2999-01-01" })).StatusCode);
    }

    [Fact]
    public async Task Illegal_transition_and_unknown_enum_are_rejected()
    {
        await using var factory = new TestApiFactory();
        using var client = factory.CreateClient();
        var created = await client.PostAsJsonAsync("/api/applications", new { company = "Example", role = "Developer", appliedOn = "2020-01-01" });
        var body = await created.Content.ReadFromJsonAsync<JsonElement>();
        var id = body.GetProperty("id").GetInt32();
        var illegal = await client.PatchAsJsonAsync($"/api/applications/{id}/status", new { status = "Offer", expectedVersion = 1 });
        Assert.Equal(HttpStatusCode.BadRequest, illegal.StatusCode);
        var unknown = await client.PatchAsJsonAsync($"/api/applications/{id}/status", new { status = "Unknown", expectedVersion = 1 });
        Assert.Equal(HttpStatusCode.BadRequest, unknown.StatusCode);
    }
}

internal sealed class TestApiFactory : WebApplicationFactory<Program>
{
    private readonly string database = Path.Combine(Path.GetTempPath(), $"careertracker-{Guid.NewGuid():N}.db");
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:CareerTracker"] = $"Data Source={database}"
        }));
    }

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
        foreach (var suffix in new[] { "", "-shm", "-wal" })
            if (File.Exists(database + suffix)) File.Delete(database + suffix);
    }
}
