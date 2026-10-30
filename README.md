# CareerTracker

A C# / .NET 10 Clean Architecture learning API for tracking job applications. The focus is not just CRUD: the aggregate enforces status transitions, clients use optimistic concurrency, and automated tests cover both business rules and real SQLite persistence.

This is an **AI-assisted portfolio sample**, inspired by [Jason Taylor's CleanArchitecture](https://github.com/jasontaylordev/CleanArchitecture). A small base entity was adapted under its MIT license; the application-specific code was written for this sample. See [third-party notices](THIRD-PARTY-NOTICES.md) for exact provenance. It is not presented as production software or independent authorship of the upstream template.

## What it demonstrates

The solution separates Domain, Application, Infrastructure, and API. Domain rules have no EF Core or HTTP dependencies. Use cases depend on a repository interface, while infrastructure provides the EF Core SQLite adapter. The API wires dependencies and returns consistent Problem Details for rule violations, missing records, and version conflicts.

Applications can be created, fetched, and listed with status filtering and bounded pagination. Status updates require the current version. EF Core checks a concurrency token as well, preventing a racing database writer from silently overwriting a change. `TimeProvider` makes date validation testable. Domain events are recorded and cleared after persistence; durable event delivery is intentionally not implemented.

## Run locally

Install the [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0), then run:

```bash
git clone https://github.com/VhutshiloMulaudzi/CareerTracker.git
cd CareerTracker
dotnet restore --locked-mode
dotnet build --no-restore --configuration Release
dotnet test --no-build --configuration Release
dotnet run --project src/CareerTracker.Api --no-launch-profile --urls http://localhost:5080
```

No Docker or external database server is required. The API creates a local `careertracker.db` file, which Git ignores. To choose a database path, set `ConnectionStrings__CareerTracker` to a SQLite connection string such as `Data Source=/tmp/careertracker.db`.

## Try the API

Use [samples/demo.http](samples/demo.http) in an HTTP client extension, or create a fictional application with curl:

```bash
curl -i http://localhost:5080/api/applications \
  -H 'Content-Type: application/json' \
  -d '{"company":"Example Software","role":"C# Backend Developer","appliedOn":"2020-01-01"}'
```

A successful create returns `201 Created`, a `Location` header, and an object like:

```json
{
  "id": 1,
  "company": "Example Software",
  "role": "C# Backend Developer",
  "appliedOn": "2020-01-01",
  "status": "Applied",
  "version": 1
}
```

Use the actual returned identifier to update its status:

```bash
curl -i -X PATCH http://localhost:5080/api/applications/1/status \
  -H 'Content-Type: application/json' \
  -d '{"status":"Interviewing","expectedVersion":1}'
```

The version becomes `2`. Sending `expectedVersion: 1` again yields `409 Conflict`. Fetch one record with `GET /api/applications/{id}`; list records with `GET /api/applications?status=Interviewing&page=1&pageSize=20`. Status names are JSON strings, not integers. Page size is limited to 100; `/health` reports process liveness only.

## Business rules

Company and role are trimmed, required, and limited to 120 and 160 characters respectively. Application dates cannot be in the future relative to the injected UTC clock.

Allowed transitions are `Applied → Interviewing / Rejected / Withdrawn`, `Interviewing → Offer / Rejected / Withdrawn`, and `Offer → Withdrawn`. Rejected and Withdrawn are terminal states. Requesting the current status is idempotent and does not increase its version. This is a deliberately small workflow; “offer accepted” is not modeled.

## Tests and CI

The initial implementation passed **25 tests** locally with a Release build: aggregate rules, application use cases using a fake repository and controlled clock, layer-reference guards, HTTP round trips over temporary SQLite databases, and competing EF Core contexts. The build completed with zero warnings or errors. A NuGet vulnerability scan of direct and transitive packages reported no known vulnerable packages at the time of the initial build; this is not a guarantee of security.

[GitHub Actions](.github/workflows/ci.yml) restores the committed lock files, builds with warnings treated as errors, runs the suite, and uploads TRX results. The workflow runs on pushes to `main` and pull requests.

## Explore the code

```text
src/
  CareerTracker.Domain/          Aggregate, validation, transitions, domain events
  CareerTracker.Application/     Repository port, use cases, DTOs
  CareerTracker.Infrastructure/  SQLite, EF configuration, concurrency adapter
  CareerTracker.Api/             HTTP endpoints and dependency composition
tests/
  CareerTracker.Tests/           Unit, architecture and integration tests
docs/
  architecture.md                Decisions and tradeoffs
  interview-guide.md             Code walkthrough and a genuine next contribution
samples/
  demo.http                      Repeatable API requests
```

Read the [architecture decisions](docs/architecture.md) for why the sample avoids unnecessary mediator/generic-repository layers, and the [interview guide](docs/interview-guide.md) to practice explaining the code rather than just listing technologies.

## Limits and next steps

This API has **no authentication or per-user data isolation**; keep it local and use fictional data. Database initialization uses `EnsureCreated`, not migrations. Before production, add authentication and ownership enforcement, reviewed migrations, rate limits, operational readiness checks, and an outbox if reliable event delivery is required. It has no UI and no deployment claim.

A useful next contribution is an interview-date feature with tests and a database migration. Make the change yourself, understand the tradeoffs, and commit it when the work actually happens.

## License

New sample code is MIT licensed in [LICENSE](LICENSE). The adapted base entity retains the upstream notice in [LICENSE-UPSTREAM](LICENSE-UPSTREAM). Built with assistance from Manus; no fabricated commit dates or activity history.
