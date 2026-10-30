using CareerTracker.Domain;

namespace CareerTracker.Application;

public record CreateApplicationRequest(string Company, string Role, DateOnly AppliedOn);

public record ChangeStatusRequest(ApplicationStatus Status, int ExpectedVersion);

public record ApplicationDto(
    int Id,
    string Company,
    string Role,
    DateOnly AppliedOn,
    ApplicationStatus Status,
    int Version);

public record PagedApplications(
    IReadOnlyList<ApplicationDto> Items,
    int TotalCount,
    int Page,
    int PageSize);
