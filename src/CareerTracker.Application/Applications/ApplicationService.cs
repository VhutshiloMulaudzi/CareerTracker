using CareerTracker.Domain;

namespace CareerTracker.Application;

public sealed class ApplicationService(IApplicationRepository repository, TimeProvider timeProvider)
{
    private readonly IApplicationRepository _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    private readonly TimeProvider _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));

    public async Task<ApplicationDto> CreateAsync(CreateApplicationRequest request, CancellationToken ct = default)
    {
        if (request is null)
        {
            throw new DomainException("Application request is required.");
        }

        var today = DateOnly.FromDateTime(_timeProvider.GetUtcNow().UtcDateTime);
        var application = JobApplication.Create(request.Company, request.Role, request.AppliedOn, today);

        await _repository.AddAsync(application, ct);
        await _repository.SaveChangesAsync(ct);

        return ToDto(application);
    }

    public async Task<ApplicationDto> GetAsync(int id, CancellationToken ct = default)
    {
        var application = await _repository.GetAsync(id, ct);
        if (application is null)
        {
            throw new NotFoundException($"Application {id} was not found.");
        }

        return ToDto(application);
    }

    public async Task<PagedApplications> ListAsync(
        ApplicationStatus? status,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        ValidateStatus(status);

        if (page < 1)
        {
            throw new DomainException("Page must be at least 1.");
        }

        if (pageSize is < 1 or > 100)
        {
            throw new DomainException("Page size must be between 1 and 100.");
        }

        var skipLong = (long)(page - 1) * pageSize;
        if (skipLong > int.MaxValue)
        {
            throw new DomainException("Page is too large.");
        }

        var applications = await _repository.ListAsync(status, (int)skipLong, pageSize, ct);
        var totalCount = await _repository.CountAsync(status, ct);

        return new PagedApplications(applications.Select(ToDto).ToArray(), totalCount, page, pageSize);
    }

    public async Task<ApplicationDto> ChangeStatusAsync(
        int id,
        ChangeStatusRequest request,
        CancellationToken ct = default)
    {
        if (request is null)
        {
            throw new DomainException("Status change request is required.");
        }

        ValidateStatus(request.Status);

        var application = await _repository.GetAsync(id, ct);
        if (application is null)
        {
            throw new NotFoundException($"Application {id} was not found.");
        }

        if (application.Version != request.ExpectedVersion)
        {
            throw new ConflictException("The application has been modified by another operation.");
        }

        application.ChangeStatus(request.Status);
        await _repository.SaveChangesAsync(ct);

        return ToDto(application);
    }

    private static void ValidateStatus(ApplicationStatus? status)
    {
        if (status is { } value && !Enum.IsDefined(typeof(ApplicationStatus), value))
        {
            throw new DomainException("Application status is invalid.");
        }
    }

    private static ApplicationDto ToDto(JobApplication application) => new(
        application.Id,
        application.Company,
        application.Role,
        application.AppliedOn,
        application.Status,
        application.Version);
}
