# Phase 1 Data Model: Project Scaffolding

This feature intentionally introduces **no domain entities**. Per spec.md FR-007 and the
Constitution Check's DEFERRED item for Principle XIII, `AuditableEntity` and every real entity
(`User`, `Kagada`, etc.) are deferred to the first feature that actually needs them — inventing
entity shapes now, with nothing yet to store, would be speculative work outside this feature's
scope.

## What exists instead

### `GariKaagadaDbContext` (in `GariKaagada.Api.Data`)

An EF Core `DbContext` subclass with **zero `DbSet<T>` properties**. Its only purpose in this
feature is to prove:
1. `GariKaagada.Api.Data` compiles and correctly references `Npgsql.EntityFrameworkCore.PostgreSQL`.
2. `GariKaagada.MigrationWorker` can construct the context, run `Database.Migrate()` against the
   self-hosted PostgreSQL container at startup, and exit cleanly with zero migrations pending.

No fields, relationships, or validation rules apply — there is nothing to model yet.

### `PingPayload` / `PingDto` (in `GariKaagada.Contracts/Ping/`)

The one placeholder pair proving the Contracts → NSwag → TypeScript pipeline (User Story 3).
Not a domain entity — a throwaway shape existing only to be pushed through the pipeline.

| Type | Field | Type | Notes |
|---|---|---|---|
| `PingPayload` (request) | `Message` | `string` | Required, max length 200 — the only rule `PingPayloadValidator` enforces, purely to prove a FluentValidation validator is wired and exhaustive-enough to be real (not a stub `NotEmpty()`-only validator masquerading as a placeholder). |
| `PingDto` (response) | `Message` | `string` | Echoes the input. |
| `PingDto` (response) | `ReceivedAtUtc` | `DateTime` | Server-set timestamp, proving the response carries server-computed data, not just an echo. |

No state transitions, no relationships — both are immutable `record` types with no persistence
(never written to `GariKaagadaDbContext`).
