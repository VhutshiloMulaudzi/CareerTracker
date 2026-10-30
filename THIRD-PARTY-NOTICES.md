# Third-party notices and provenance

## Jason Taylor's CleanArchitecture

Source: https://github.com/jasontaylordev/CleanArchitecture

Reviewed source commit: `345e7fe7612fdaf0cb5da58826a7c57d42e1fdd8`.

Adapted file: upstream `src/Domain/Common/BaseEntity.cs` to this project's `src/CareerTracker.Domain/Common/BaseEntity.cs`.

The adaptation retains an integer entity identifier and the list-backed add/remove/clear domain-event collection. It changes the namespace and event type, restricts identifier mutation, and removes EF-specific mapping attributes; the infrastructure model ignores domain events instead. The original copyright and MIT permission notice are preserved in `LICENSE-UPSTREAM`.

The layered layout is inspired by this template. CareerTracker is not a complete copy, fork, or official extension of the upstream project. The job-application aggregate, use cases, API, persistence adapter, tests, and documentation were written for this AI-assisted learning sample.

## Secondary reference

https://github.com/ardalis/CleanArchitecture was reviewed as a second public Clean Architecture example. No source files from it are included.

## AI assistance

Manus assisted with research, implementation, tests, and documentation. This repository describes a learning/portfolio sample and makes no claim that its owner independently authored all code or that it is a production system.

NuGet package licenses remain with their respective authors and can be inspected through the pinned package references and lock files.
