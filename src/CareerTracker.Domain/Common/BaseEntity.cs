// Adapted from JasonTaylorDev/CleanArchitecture (MIT License).
// Copyright (c) Jason Taylor.

namespace CareerTracker.Domain;

public abstract class BaseEntity
{
    private readonly List<DomainEvent> _domainEvents = [];

    public int Id { get; protected set; }

    public IReadOnlyCollection<DomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    public void AddDomainEvent(DomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }

    public void RemoveDomainEvent(DomainEvent domainEvent)
    {
        _domainEvents.Remove(domainEvent);
    }

    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
}
