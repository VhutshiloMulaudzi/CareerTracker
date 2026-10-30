using System.Text.Json.Serialization;
using CareerTracker.Application;
using CareerTracker.Domain;
using CareerTracker.Infrastructure;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddProblemDetails();
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter<ApplicationStatus>(allowIntegerValues: false)));
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("CareerTracker") ?? "Data Source=careertracker.db"));
builder.Services.AddScoped<IApplicationRepository, EfApplicationRepository>();
builder.Services.AddScoped<ApplicationService>();

var app = builder.Build();
app.UseExceptionHandler(errorApp => errorApp.Run(async context =>
{
    var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;
    var (status, title) = exception switch
    {
        DomainException => (StatusCodes.Status400BadRequest, "Validation failed"),
        NotFoundException => (StatusCodes.Status404NotFound, "Application not found"),
        ConflictException => (StatusCodes.Status409Conflict, "Version conflict"),
        _ => (StatusCodes.Status500InternalServerError, "Unexpected error")
    };
    await Results.Problem(statusCode: status, title: title,
        detail: status == 500 ? "An unexpected error occurred." : exception?.Message).ExecuteAsync(context);
}));

// Local demo bootstrap only. Production should use reviewed migrations.
using (var scope = app.Services.CreateScope())
    await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Database.EnsureCreatedAsync();

app.MapGet("/", () => Results.Ok(new { name = "CareerTracker", version = "1.0", docs = "See README.md and samples/demo.http" }));
app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));
var applications = app.MapGroup("/api/applications");
applications.MapPost("/", async (CreateApplicationRequest request, ApplicationService service, CancellationToken ct) =>
{
    var result = await service.CreateAsync(request, ct);
    return Results.Created($"/api/applications/{result.Id}", result);
});
applications.MapGet("/{id:int}", async (int id, ApplicationService service, CancellationToken ct) =>
    Results.Ok(await service.GetAsync(id, ct)));
applications.MapGet("/", async (ApplicationStatus? status, int? page, int? pageSize, ApplicationService service, CancellationToken ct) =>
    Results.Ok(await service.ListAsync(status, page ?? 1, pageSize ?? 20, ct)));
applications.MapPatch("/{id:int}/status", async (int id, ChangeStatusRequest request, ApplicationService service, CancellationToken ct) =>
    Results.Ok(await service.ChangeStatusAsync(id, request, ct)));
app.Run();

public partial class Program;
