using CareerTracker.Application;
using CareerTracker.Domain;
using Microsoft.EntityFrameworkCore;

namespace CareerTracker.Infrastructure;

public sealed class EfApplicationRepository(ApplicationDbContext db) : IApplicationRepository
{
    public async Task AddAsync(JobApplication entity, CancellationToken ct) =>
        await db.Applications.AddAsync(entity, ct);

    public Task<JobApplication?> GetAsync(int id, CancellationToken ct) =>
        db.Applications.SingleOrDefaultAsync(x => x.Id == id, ct);

    public async Task<IReadOnlyList<JobApplication>> ListAsync(ApplicationStatus? status, int skip, int take, CancellationToken ct) =>
        await Filter(status).AsNoTracking().OrderByDescending(x => x.AppliedOn)
            .ThenByDescending(x => x.Id).Skip(skip).Take(take).ToListAsync(ct);

    public Task<int> CountAsync(ApplicationStatus? status, CancellationToken ct) => Filter(status).CountAsync(ct);

    public async Task SaveChangesAsync(CancellationToken ct)
    {
        try
        {
            await db.SaveChangesAsync(ct);
            // Events are in-process observations only; no durable event delivery is claimed.
            foreach (var entry in db.ChangeTracker.Entries<JobApplication>())
                entry.Entity.ClearDomainEvents();
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConflictException("This application changed. Reload it and retry with its current version.");
        }
    }

    private IQueryable<JobApplication> Filter(ApplicationStatus? status) =>
        status is null ? db.Applications : db.Applications.Where(x => x.Status == status);
}
