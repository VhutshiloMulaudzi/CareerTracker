namespace CareerTracker.Domain;

public abstract record DomainEvent;

public record ApplicationStatusChangedEvent(
    int ApplicationId,
    ApplicationStatus Previous,
    ApplicationStatus Current) : DomainEvent;
