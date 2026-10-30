using CareerTracker.Domain;

namespace CareerTracker.Tests;

public sealed class DomainTests
{
    private static readonly DateOnly Today = new(2026, 10, 30);

    [Fact]
    public void Create_TrimsValuesAndInitializesAggregate()
    {
        var application = JobApplication.Create("  Northwind  ", "  Software Engineer  ", Today, Today);

        Assert.Equal("Northwind", application.Company);
        Assert.Equal("Software Engineer", application.Role);
        Assert.Equal(Today, application.AppliedOn);
        Assert.Equal(ApplicationStatus.Applied, application.Status);
        Assert.Equal(1, application.Version);
        Assert.Empty(application.DomainEvents);
    }

    [Theory]
    [InlineData(null, "Engineer")]
    [InlineData("   ", "Engineer")]
    [InlineData("Contoso", " ")]
    public void Create_RejectsMissingText(string? company, string? role)
    {
        Assert.Throws<DomainException>(() => JobApplication.Create(company!, role!, Today, Today));
    }

    [Fact]
    public void Create_RejectsFutureAppliedDateAndExcessiveText()
    {
        Assert.Throws<DomainException>(() => JobApplication.Create("Contoso", "Engineer", Today.AddDays(1), Today));
        Assert.Throws<DomainException>(() => JobApplication.Create(new string('c', 121), "Engineer", Today, Today));
        Assert.Throws<DomainException>(() => JobApplication.Create("Contoso", new string('r', 161), Today, Today));
    }

    [Fact]
    public void ChangeStatus_ChangesVersionAndRaisesEvent()
    {
        var application = JobApplication.Create("Contoso", "Engineer", Today, Today);

        application.ChangeStatus(ApplicationStatus.Interviewing);

        Assert.Equal(ApplicationStatus.Interviewing, application.Status);
        Assert.Equal(2, application.Version);
        var statusChanged = Assert.IsType<ApplicationStatusChangedEvent>(Assert.Single(application.DomainEvents));
        Assert.Equal(application.Id, statusChanged.ApplicationId);
        Assert.Equal(ApplicationStatus.Applied, statusChanged.Previous);
        Assert.Equal(ApplicationStatus.Interviewing, statusChanged.Current);
    }

    [Fact]
    public void ChangeStatus_IsIdempotentForSameStatus()
    {
        var application = JobApplication.Create("Contoso", "Engineer", Today, Today);

        application.ChangeStatus(ApplicationStatus.Applied);

        Assert.Equal(1, application.Version);
        Assert.Empty(application.DomainEvents);
    }

    [Fact]
    public void ChangeStatus_RejectsInvalidTransitionsAndTerminalStates()
    {
        var application = JobApplication.Create("Contoso", "Engineer", Today, Today);
        Assert.Throws<DomainException>(() => application.ChangeStatus(ApplicationStatus.Offer));

        application.ChangeStatus(ApplicationStatus.Interviewing);
        application.ChangeStatus(ApplicationStatus.Rejected);

        Assert.Throws<DomainException>(() => application.ChangeStatus(ApplicationStatus.Offer));
        Assert.Equal(3, application.Version);
    }

    [Theory]
    [InlineData(ApplicationStatus.Rejected)]
    [InlineData(ApplicationStatus.Withdrawn)]
    public void ChangeStatus_AllowsRejectionOrWithdrawalFromApplied(ApplicationStatus status)
    {
        var application = JobApplication.Create("Contoso", "Engineer", Today, Today);

        application.ChangeStatus(status);

        Assert.Equal(status, application.Status);
        Assert.Equal(2, application.Version);
    }

    [Fact]
    public void ChangeStatus_AllowsInterviewOfferAndWithdrawalProgression()
    {
        var application = JobApplication.Create("Contoso", "Engineer", Today, Today);

        application.ChangeStatus(ApplicationStatus.Interviewing);
        application.ChangeStatus(ApplicationStatus.Offer);
        application.ChangeStatus(ApplicationStatus.Withdrawn);

        Assert.Equal(ApplicationStatus.Withdrawn, application.Status);
        Assert.Equal(4, application.Version);
    }

    [Fact]
    public void ChangeStatus_RejectsUndefinedStatus()
    {
        var application = JobApplication.Create("Contoso", "Engineer", Today, Today);

        Assert.Throws<DomainException>(() => application.ChangeStatus((ApplicationStatus)999));
    }
}
