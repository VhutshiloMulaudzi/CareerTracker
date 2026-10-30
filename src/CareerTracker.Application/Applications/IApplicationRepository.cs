using CareerTracker.Domain;

namespace CareerTracker.Application;

public interface IApplicationRepository
{
    Task AddAsync(JobApplication entity, CancellationToken ct);

    Task<JobApplication?> GetAsync(int id, CancellationToken ct);

    Task<IReadOnlyList<JobApplication>> ListAsync(ApplicationStatus? status, int skip, int take, CancellationToken ct);

    Task<int> CountAsync(ApplicationStatus? status, CancellationToken ct);

    Task SaveChangesAsync(CancellationToken ct);
}
