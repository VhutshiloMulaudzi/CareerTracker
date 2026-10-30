using System.Reflection;
using CareerTracker.Domain;
using CareerTracker.Application;

namespace CareerTracker.Tests;

public sealed class ArchitectureTests
{
    [Fact]
    public void Domain_has_no_outward_or_framework_dependencies()
    {
        var references = typeof(JobApplication).Assembly.GetReferencedAssemblies().Select(x => x.Name!).ToArray();
        Assert.DoesNotContain(references, name => name.StartsWith("CareerTracker.", StringComparison.Ordinal));
        Assert.DoesNotContain(references, name => name.StartsWith("Microsoft.EntityFrameworkCore", StringComparison.Ordinal));
        Assert.DoesNotContain(references, name => name.StartsWith("Microsoft.AspNetCore", StringComparison.Ordinal));
    }

    [Fact]
    public void Application_does_not_reference_infrastructure_or_api()
    {
        var references = typeof(ApplicationService).Assembly.GetReferencedAssemblies().Select(x => x.Name!).ToArray();
        Assert.Contains("CareerTracker.Domain", references);
        Assert.DoesNotContain("CareerTracker.Infrastructure", references);
        Assert.DoesNotContain("CareerTracker.Api", references);
        Assert.DoesNotContain(references, name => name.StartsWith("Microsoft.EntityFrameworkCore", StringComparison.Ordinal));
    }
}
