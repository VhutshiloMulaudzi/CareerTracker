using CareerTracker.Application;
using CareerTracker.Domain;

namespace CareerTracker.Tests;

public sealed class ApplicationTests
{
    private static readonly DateOnly Today = new(2026, 10, 30);
    private static readonly TimeProvider Clock = new FixedTimeProvider(
        new DateTimeOffset(2026, 10, 30, 16, 0, 0, TimeSpan.Zero));

    [Fact]
    public async Task CreateAsync_AddsAndSavesTrimmedApplication()
    {
        var repository = new FakeApplicationRepository();
        var service = new ApplicationService(repository, Clock);

        var result = await service.CreateAsync(new CreateApplicationRequest("  Fabrikam ", "  Developer ", Today));

        Assert.Equal(1, result.Id);
        Assert.Equal("Fabrikam", result.Company);
        Assert.Equal("Developer", result.Role);
        Assert.Equal(1, repository.AddCalls);
        Assert.Equal(1, repository.SaveCalls);
    }

    [Fact]
    public async Task CreateAsync_UsesUtcDateForFutureDateValidation()
    {
        var service = new ApplicationService(new FakeApplicationRepository(), Clock);

        await Assert.ThrowsAsync<DomainException>(() => service.CreateAsync(
            new CreateApplicationRequest("Fabrikam", "Developer", Today.AddDays(1))));
    }

    [Fact]
    public async Task GetAsync_ThrowsNotFoundForUnknownApplication()
    {
        var service = new ApplicationService(new FakeApplicationRepository(), Clock);

        await Assert.ThrowsAsync<NotFoundException>(() => service.GetAsync(404));
    }

    [Fact]
    public async Task ListAsync_AppliesFilterAndReturnsPaginationMetadata()
    {
        var repository = new FakeApplicationRepository();
        await repository.AddAsync(JobApplication.Create("Applied", "Developer", Today, Today), CancellationToken.None);
        var interviewing = JobApplication.Create("Interviewing", "Developer", Today, Today);
        interviewing.ChangeStatus(ApplicationStatus.Interviewing);
        await repository.AddAsync(interviewing, CancellationToken.None);
        var service = new ApplicationService(repository, Clock);

        var result = await service.ListAsync(ApplicationStatus.Interviewing, 1, 10);

        var item = Assert.Single(result.Items);
        Assert.Equal("Interviewing", item.Company);
        Assert.Equal(1, result.TotalCount);
        Assert.Equal(1, result.Page);
        Assert.Equal(10, result.PageSize);
    }

    [Fact]
    public async Task ListAsync_RejectsInvalidFilterAndPagination()
    {
        var service = new ApplicationService(new FakeApplicationRepository(), Clock);

        await Assert.ThrowsAsync<DomainException>(() => service.ListAsync((ApplicationStatus)999, 1, 10));
        await Assert.ThrowsAsync<DomainException>(() => service.ListAsync(null, 0, 10));
        await Assert.ThrowsAsync<DomainException>(() => service.ListAsync(null, 1, 101));
    }

    [Fact]
    public async Task ChangeStatusAsync_ChecksVersionThenSavesTransition()
    {
        var repository = new FakeApplicationRepository();
        var application = JobApplication.Create("Contoso", "Engineer", Today, Today);
        await repository.AddAsync(application, CancellationToken.None);
        var service = new ApplicationService(repository, Clock);

        await Assert.ThrowsAsync<ConflictException>(() => service.ChangeStatusAsync(
            application.Id,
            new ChangeStatusRequest(ApplicationStatus.Interviewing, ExpectedVersion: 2)));
        Assert.Equal(0, repository.SaveCalls);

        var result = await service.ChangeStatusAsync(
            application.Id,
            new ChangeStatusRequest(ApplicationStatus.Interviewing, ExpectedVersion: 1));

        Assert.Equal(ApplicationStatus.Interviewing, result.Status);
        Assert.Equal(2, result.Version);
        Assert.Equal(1, repository.SaveCalls);
    }

    [Fact]
    public async Task ChangeStatusAsync_RejectsUndefinedStatusBeforeRepositoryLookup()
    {
        var repository = new FakeApplicationRepository();
        var service = new ApplicationService(repository, Clock);

        await Assert.ThrowsAsync<DomainException>(() => service.ChangeStatusAsync(
            123,
            new ChangeStatusRequest((ApplicationStatus)999, 1)));
        Assert.Equal(0, repository.GetCalls);
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }

    private sealed class FakeApplicationRepository : IApplicationRepository
    {
        private readonly Dictionary<int, JobApplication> _applications = [];
        private int _nextId = 1;

        public int AddCalls { get; private set; }
        public int GetCalls { get; private set; }
        public int SaveCalls { get; private set; }

        public Task AddAsync(JobApplication entity, CancellationToken ct)
        {
            SetId(entity, _nextId++);
            _applications.Add(entity.Id, entity);
            AddCalls++;
            return Task.CompletedTask;
        }

        public Task<JobApplication?> GetAsync(int id, CancellationToken ct)
        {
            GetCalls++;
            _applications.TryGetValue(id, out var application);
            return Task.FromResult(application);
        }

        public Task<IReadOnlyList<JobApplication>> ListAsync(
            ApplicationStatus? status,
            int skip,
            int take,
            CancellationToken ct)
        {
            IReadOnlyList<JobApplication> applications = _applications.Values
                .Where(application => status is null || application.Status == status)
                .OrderBy(application => application.Id)
                .Skip(skip)
                .Take(take)
                .ToArray();
            return Task.FromResult(applications);
        }

        public Task<int> CountAsync(ApplicationStatus? status, CancellationToken ct) =>
            Task.FromResult(_applications.Values.Count(application => status is null || application.Status == status));

        public Task SaveChangesAsync(CancellationToken ct)
        {
            SaveCalls++;
            return Task.CompletedTask;
        }

        private static void SetId(JobApplication application, int id) =>
            typeof(BaseEntity).GetProperty(nameof(BaseEntity.Id))!.SetValue(application, id);
    }
}
