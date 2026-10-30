namespace CareerTracker.Domain;

public sealed class JobApplication : BaseEntity
{
    private JobApplication()
    {
    }

    public string Company { get; private set; } = null!;

    public string Role { get; private set; } = null!;

    public DateOnly AppliedOn { get; private set; }

    public ApplicationStatus Status { get; private set; } = ApplicationStatus.Applied;

    public int Version { get; private set; } = 1;

    public static JobApplication Create(string company, string role, DateOnly appliedOn, DateOnly today)
    {
        if (appliedOn > today)
        {
            throw new DomainException("Applied date cannot be in the future.");
        }

        return new JobApplication
        {
            Company = ValidateText(company, "Company", 120),
            Role = ValidateText(role, "Role", 160)
        }.WithAppliedOn(appliedOn);
    }

    public void ChangeStatus(ApplicationStatus status)
    {
        if (!Enum.IsDefined(typeof(ApplicationStatus), status))
        {
            throw new DomainException("Application status is invalid.");
        }

        if (Status == status)
        {
            return;
        }

        if (!CanTransitionTo(status))
        {
            throw new DomainException($"Cannot change application status from {Status} to {status}.");
        }

        var previous = Status;
        Status = status;
        Version++;
        AddDomainEvent(new ApplicationStatusChangedEvent(Id, previous, status));
    }

    private JobApplication WithAppliedOn(DateOnly appliedOn)
    {
        AppliedOn = appliedOn;
        return this;
    }

    private bool CanTransitionTo(ApplicationStatus status) => Status switch
    {
        ApplicationStatus.Applied => status is ApplicationStatus.Interviewing or ApplicationStatus.Rejected or ApplicationStatus.Withdrawn,
        ApplicationStatus.Interviewing => status is ApplicationStatus.Offer or ApplicationStatus.Rejected or ApplicationStatus.Withdrawn,
        ApplicationStatus.Offer => status is ApplicationStatus.Withdrawn,
        _ => false
    };

    private static string ValidateText(string? value, string fieldName, int maximumLength)
    {
        var trimmed = value?.Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            throw new DomainException($"{fieldName} is required.");
        }

        if (trimmed.Length > maximumLength)
        {
            throw new DomainException($"{fieldName} must not exceed {maximumLength} characters.");
        }

        return trimmed;
    }
}
