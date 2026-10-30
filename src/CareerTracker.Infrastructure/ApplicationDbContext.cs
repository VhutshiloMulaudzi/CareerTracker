using CareerTracker.Domain;
using Microsoft.EntityFrameworkCore;

namespace CareerTracker.Infrastructure;

public sealed class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    public DbSet<JobApplication> Applications => Set<JobApplication>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var application = modelBuilder.Entity<JobApplication>();
        application.HasKey(x => x.Id);
        application.Property(x => x.Company).IsRequired().HasMaxLength(120);
        application.Property(x => x.Role).IsRequired().HasMaxLength(160);
        application.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
        application.Property(x => x.Version).IsConcurrencyToken();
        application.Ignore(x => x.DomainEvents);
        application.HasIndex(x => new { x.Status, x.AppliedOn });
    }
}
