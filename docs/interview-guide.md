# Explain the project in an interview

This is an AI-assisted learning project, not a claim of commercial delivery. Run it, inspect the code, and make a change you can explain before presenting it as evidence of your understanding.

## Walk through a status update

Start at the PATCH endpoint in `Program.cs`. It delegates to `ApplicationService`, which loads the aggregate through `IApplicationRepository`, checks the expected version, and calls `JobApplication.ChangeStatus`. The aggregate validates the transition and increments its version. The SQLite adapter saves it; EF Core protects against concurrent updates. The API returns 400 for a rule violation, 404 for a missing record, or 409 for a version conflict.

## Questions worth answering from the code

Why does the domain not reference EF Core? Why is a version check in the use case insufficient by itself? Why use a real SQLite database for integration tests? Why is `TimeProvider` injected? Why does the same-status request not increment the version? What are the limitations of clearing domain events without an outbox? Why is this repository specific to one aggregate rather than generic?

## Make a genuine follow-up contribution

Add an optional interview date as your own next change. Decide whether dates before the application date should be allowed, update the aggregate and DTOs, write failing tests first, and add a migration rather than deleting the database. Commit that change when it is actually made; do not backdate commits or pad the contribution graph.

A concise, accurate description is: “A C#/.NET Clean Architecture learning API with encapsulated status rules, SQLite persistence, optimistic concurrency and automated tests. Adapted a small MIT-licensed base entity and used AI assistance; the architecture notes explain the tradeoffs.”
