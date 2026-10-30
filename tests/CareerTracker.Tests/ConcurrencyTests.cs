using CareerTracker.Application;
using CareerTracker.Domain;
using CareerTracker.Infrastructure;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace CareerTracker.Tests;

public sealed class ConcurrencyTests
{
    [Fact]
    public async Task Two_contexts_cannot_silently_overwrite_the_same_version()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options;
        await using var first = new ApplicationDbContext(options);
        await first.Database.EnsureCreatedAsync();
        var application = JobApplication.Create("Example", "Developer", new DateOnly(2020, 1, 1), new DateOnly(2020, 1, 1));
        first.Applications.Add(application);
        await first.SaveChangesAsync();
        await using var second = new ApplicationDbContext(options);
        var competing = await second.Applications.SingleAsync();
        application.ChangeStatus(ApplicationStatus.Interviewing);
        await new EfApplicationRepository(first).SaveChangesAsync(default);
        competing.ChangeStatus(ApplicationStatus.Rejected);
        await Assert.ThrowsAsync<ConflictException>(() => new EfApplicationRepository(second).SaveChangesAsync(default));
    }
}
