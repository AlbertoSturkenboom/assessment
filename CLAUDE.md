# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Overview

A .NET 10 background-job system: a Minimal API accepts jobs, a Worker processes
them asynchronously, and both share an in-memory queue and job store through a
common Core library.

## Commands

```bash
dotnet build                      # build the whole solution (Assessment.sln)
dotnet test                       # run all tests (Core.Tests, xUnit)
dotnet run --project Api          # run the API; this ALSO runs the Worker (see Architecture)

# Run a single test by name (xUnit FQN or display-name substring):
dotnet test --filter "FullyQualifiedName~JobQueueTests.Dequeue_ReturnsJobsInFifoOrder"
dotnet test --filter "DisplayName~ConcurrentReadsAndWrites"

dotnet list Core.Tests/Core.Tests.csproj package --vulnerable --include-transitive
dotnet format                     # apply the style rules from .editorconfig
```

## Architecture

Three projects plus tests; the dependency direction is `Api → Worker → Core`,
with `Api → Core` and `Core.Tests → Core`.

- **Core** — domain + infrastructure abstractions, with no dependency on the
  hosts. `BackgroundJob` is an **immutable `record`**; state changes go through
  `MarkProcessing`/`MarkCompleted`/`MarkFailed`, each returning a *new* snapshot
  (and clearing the opposite terminal field). `IJobQueue`/`JobQueue` wrap an
  unbounded `System.Threading.Channels` channel; `IJobStore`/`InMemoryJobStore`
  wrap a `ConcurrentDictionary`. `ServiceCollectionExtensions.AddJobInfrastructure`
  is the single registration point for both as singletons.

- **Api** — Minimal API. Endpoints live in `JobEndpoints.MapJobEndpoints`
  (`POST /api/jobs` → 202 + Location; `GET /api/jobs/{id}`). `Program.cs` is the
  composition root.

- **Worker** — `MainWorker` is a `BackgroundService` that runs
  `Worker:MaxConcurrency` (default 4) parallel consumer loops over the shared
  queue and writes status snapshots to the shared store.

### The key non-obvious design point

The queue and store are **in-memory, per-process singletons**. For a job
enqueued by the API to actually be processed, the producer (API endpoints) and
the consumer (Worker) must live in the **same process**. That is why `Api`
references `Worker` and registers it via `AddHostedService<MainWorker>()` — so
the endpoints and the Worker share the *same* `IJobQueue`/`IJobStore`
instances. Running the standalone `Worker` project as a separate process gives
it its own empty queue/store and will NOT process the API's jobs.

### Concurrency model

Safe concurrent reads/writes rely on safe publication: each `BackgroundJob`
snapshot is fully constructed before it is swapped atomically into the
`ConcurrentDictionary`, so a reader (API `GET`) always sees an internally
consistent snapshot. Do not reintroduce mutable shared state on `BackgroundJob`.
The `CancellationToken` is threaded through dequeue and the simulated work;
`ProcessAsync` catches `OperationCanceledException` *before* the generic
`catch` so a job cancelled during shutdown is not misclassified as `Failed`.

## Build / runtime notes

- Targets `net10.0`. `Directory.Build.props` sets `RollForward=Major`, so the
  apps/tests also run on a newer major runtime when the exact one is absent.
- `Directory.Build.props` enables `EnforceCodeStyleInBuild` and
  `GenerateDocumentationFile` (with `CS1591` suppressed) so analyzer/style rules
  — including IDE0005 unused-usings — run during `dotnet build`.
- The root `.editorconfig` is the shared source of truth for style (Rider and
  the build read it). `Core/.editorconfig` additionally enables `CA2007` for the
  library only.

## Git & Commit Instructions

- Create commits after a logical unit of work (feature/fix) has been fully
  completed and tested.
- Use Conventional Commits (`feat:`, `fix:`, `refactor:`, `chore:`).
- Do not perform git pushes; always perform pushes manually after your own
  verification.
