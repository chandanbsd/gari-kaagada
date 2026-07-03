---

description: "Task list for implementing Project Scaffolding"

---

# Tasks: Project Scaffolding

**Input**: Design documents from `/specs/001-project-scaffolding/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/, quickstart.md (all present)

**Tests**: Not included — constitution Principle XV (No Test Projects) prohibits test
projects/files entirely, and this was not overridden for this feature.

**Organization**: Tasks are grouped by user story (spec.md) to enable independent
implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies on incomplete tasks)
- **[Story]**: Which user story this task belongs to (US1, US2, US3)
- Every task includes its exact file path(s)

## Path Conventions

Paths match plan.md's Project Structure exactly — this is a multi-project .NET solution (one
top-level `.slnx` with `BFF`/`Api` solution folders) plus one standalone Angular project
(`gari-kagada-client/`), not a generic single-project or two-folder web-app layout.

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Solution-wide files that every project depends on, before any individual project
exists.

- [X] T001 Create `GariKaagada.slnx` at the repository root (XML solution format — see
      research.md) with two empty solution folders, `BFF` and `Api`, ready for projects to be
      added in later tasks.
- [X] T002 [P] Create `Directory.Packages.props` at the repository root with
      `<ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>` and a
      `<PackageVersion>` entry for every third-party package identified in plan.md's Primary
      Dependencies (ASP.NET Core/EF Core/Npgsql packages pulled in via the SDK or explicit
      `PackageReference`, `FluentValidation`, `NSwag.AspNetCore`, `NSwag.MSBuild`,
      `OpenTelemetry.*`, `Microsoft.Extensions.ServiceDiscovery`,
      `Microsoft.Extensions.Http.Resilience`, and the Aspire hosting packages needed by the
      AppHost). No individual `.csproj` created later may specify its own `Version` attribute.
- [X] T003 [P] Create `Directory.Build.props` at the repository root setting
      `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>` and `<Nullable>enable</Nullable>`
      for every .NET project in the solution (constitution Principle X).
- [X] T004 [P] Verify `nuget.config` at the repository root still resolves packages correctly
      once Central Package Management is enabled (no content change expected — confirm
      compatibility only).

**Checkpoint**: Solution-wide files exist; ready to add individual projects.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Every project every user story needs to exist, correctly wired per constitution
Principle VII, before any story-specific work (AppHost wiring, illegal-reference proof, or the
Ping pipeline) can begin.

**⚠️ CRITICAL**: No user story work can begin until this phase is complete.

- [X] T005 [P] Create `GariKaagada.ServiceDefaults` class library at
      `GariKaagada.ServiceDefaults/GariKaagada.ServiceDefaults.csproj`, with `Extensions.cs`
      implementing `AddServiceDefaults`, `ConfigureOpenTelemetry`, `AddDefaultHealthChecks`, and
      `MapDefaultEndpoints` exactly per the official Aspire ServiceDefaults template shape
      (research.md) — zero dependencies on any other solution project.
- [X] T006 [P] Create `GariKaagada.Contracts` class library at
      `GariKaagada.Contracts/GariKaagada.Contracts.csproj` — empty for now (the `Ping`
      Payload/Dto pair is added in Phase 5 / US3); zero dependencies on any other solution
      project.
- [X] T007 [P] Create `GariKaagada.Api.Data` class library at
      `Api/GariKaagada.Api.Data/GariKaagada.Api.Data.csproj`, referencing `GariKaagada.Contracts`
      only, with `GariKaagadaDbContext.cs` — an EF Core `DbContext` with **zero `DbSet`
      properties** — and the `Npgsql.EntityFrameworkCore.PostgreSQL` provider configured
      (data-model.md).
- [X] T008 [P] Create `GariKaagada.Api.Business` class library at
      `Api/GariKaagada.Api.Business/GariKaagada.Api.Business.csproj`, referencing
      `GariKaagada.Api.Data` and `GariKaagada.Contracts` — empty (depends on T006, T007).
- [X] T009 [P] Create `GariKaagada.Api` ASP.NET Core Web API project at
      `Api/GariKaagada.Api/GariKaagada.Api.csproj`, referencing `GariKaagada.Api.Business`,
      `GariKaagada.Contracts`, and `GariKaagada.ServiceDefaults` only, with `Program.cs` calling
      `AddServiceDefaults()`/`MapDefaultEndpoints()` (depends on T005, T006, T008).
- [X] T010 [P] Create `GariKaagada.BFF.Business` class library at
      `BFF/GariKaagada.BFF.Business/GariKaagada.BFF.Business.csproj`, referencing
      `GariKaagada.Contracts` only — empty (depends on T006).
- [X] T011 [P] Create `GariKaagada.BFF` ASP.NET Core Web API project at
      `BFF/GariKaagada.BFF/GariKaagada.BFF.csproj`, referencing `GariKaagada.BFF.Business`,
      `GariKaagada.Contracts`, and `GariKaagada.ServiceDefaults` only, with `Program.cs` calling
      `AddServiceDefaults()`/`MapDefaultEndpoints()` (depends on T005, T006, T010).
- [X] T012 Create `GariKaagada.MigrationWorker` Aspire worker service at
      `GariKaagada.MigrationWorker/GariKaagada.MigrationWorker.csproj`, referencing
      `GariKaagada.Api.Data` and `GariKaagada.ServiceDefaults`, with `Program.cs` running EF Core
      migrations against `GariKaagadaDbContext` at startup (depends on T005, T007).
- [X] T013 Add all seven projects created in T005–T012 to `GariKaagada.slnx`, under the correct
      solution folder (`BFF` or `Api`; `ServiceDefaults`, `Contracts`, and `MigrationWorker`
      stay at solution root per plan.md's Project Structure) (depends on T001, T005–T012).
- [X] T014 [P] Scaffold `gari-kagada-client/` at the repository root via `ng new` (standalone
      components, `"strict": true` in `tsconfig.json`), then `ng add @angular/material`
      selecting the Custom theme option, then run the `ng generate @angular/material:m3-theme`
      schematic to produce a from-scratch M3 theme file (research.md; constitution Principle
      III — no hard-coded hex values, no pre-built non-token CSS).

**Checkpoint**: Every constitution-mandated project exists, compiles standalone, and is wired
per the layering rules. User Stories 1, 2, and 3 can now all begin.

---

## Phase 3: User Story 1 - Start the full local stack with one command (Priority: P1) 🎯 MVP

**Goal**: `aspire run` brings up every project and backing container (PostgreSQL, Keycloak,
SigNoz stack, `gari-kagada-client`) to a healthy/running state, idempotently, with one command.

**Independent Test**: Clone the repo, run `aspire run`, observe every resource reach
healthy/running in the Aspire dashboard (per quickstart.md).

### Implementation for User Story 1

- [X] T015 [US1] Create `GariKaagada.AppHost` project at
      `GariKaagada.AppHost/GariKaagada.AppHost.csproj`; add it to `GariKaagada.slnx` (solution
      root, per plan.md) and to the packages governed by `Directory.Packages.props`.
- [X] T016 [US1] Retire the repository-root single-file `apphost.cs` AppHost and update
      `aspire.config.json`'s `appHost.path` to point at
      `GariKaagada.AppHost/GariKaagada.AppHost.csproj` instead (research.md decision).
- [X] T017 [US1] In `GariKaagada.AppHost`, declare the PostgreSQL container pinned to a
      major-version image tag (e.g., `postgres:16` — confirm current major version at
      implementation time), with `WithLifetime(ContainerLifetime.Persistent)` and a named data
      volume (research.md idempotent-restart decision; spec.md FR-009, SC-006).
- [X] T018 [US1] In `GariKaagada.AppHost`, declare the Keycloak container pinned to a
      major-version image tag, with `WithLifetime(ContainerLifetime.Persistent)` and a named
      data volume (same rationale as T017).
- [X] T019 [US1] In `GariKaagada.AppHost`, declare the SigNoz-stack containers — confirm the
      current topology against SigNoz's official self-host docs first (research.md flags this
      as unresolved/drift-prone) — each pinned to a major-version image tag, with
      `WithLifetime(ContainerLifetime.Persistent)` and named data volumes.
- [X] T020 [US1] In `GariKaagada.AppHost`, add `GariKaagada.Api` as a project resource,
      `WaitFor` the PostgreSQL container (T017), and add `GariKaagada.MigrationWorker` as a
      project resource with `WaitForCompletion` gating `GariKaagada.Api`'s startup.
- [X] T021 [US1] In `GariKaagada.AppHost`, add `GariKaagada.BFF` as a project resource with
      `WithReference` to `GariKaagada.Api` (Aspire service discovery) and to the Keycloak
      container (T018).
- [X] T022 [US1] In `GariKaagada.AppHost`, orchestrate `gari-kagada-client` via
      `AddJavaScriptApp` (or `AddNpmApp` fallback — confirm which is available per research.md)
      with `WithReference` to `GariKaagada.BFF` and an HTTP health check against its dev server
      endpoint (clarification 2026-07-03: frontend "healthy" = a real HTTP check, not just
      process-running state).
- [X] T023 [US1] In `BFF/GariKaagada.BFF/Program.cs`, register an `HttpClient` against
      `GariKaagada.Api` resolved via Aspire service discovery, proving the BFF → Api internal
      transport is wired end-to-end (constitution Principle VI).
- [X] T024 [US1] In `BFF/GariKaagada.BFF/`, add an empty `AppHub : Hub` class at
      `Hubs/AppHub.cs` (no hub methods) and map it at `/hubs/app` in `Program.cs`, proving the
      SignalR hub-hosting mechanism itself works (constitution Principle V/VI, structural only).
- [X] T025 [US1] Update `README.md` with the Podman/Aspire prerequisites: Podman ≥5.0.0
      installed, `ASPIRE_CONTAINER_RUNTIME=podman` exported, and `aspire doctor` as the
      recommended first post-clone command (constitution Principle II).
- [X] T026 [US1] Run quickstart.md's "Validate User Story 1" and "Validate idempotent restart"
      sections end-to-end: confirm every resource is healthy within 5 minutes of `aspire run`
      (SC-001) including the frontend's real HTTP health check, confirm stopping leaves zero
      orphaned containers (SC-005), and confirm a force-killed mid-startup attempt can be
      re-run with zero manual cleanup, reusing (not recreating) the backing containers
      (SC-006).

**Checkpoint**: The full local stack starts, is healthy, and survives a partial-failure restart
— User Story 1 is independently complete and demonstrable.

---

## Phase 4: User Story 2 - Illegal cross-tier references fail at build time (Priority: P2)

**Goal**: Prove that the project layering established in Phase 2 actually rejects illegal
references at build time, not just in code review.

**Independent Test**: Attempt an upward project reference that violates the mandated layering
and confirm the build fails (per quickstart.md); no new production code is added by this story
— it validates work already done in Phase 2.

### Implementation for User Story 2

- [X] T027 [US2] Following quickstart.md's "Validate User Story 2" steps: temporarily add an
      illegal `ProjectReference` from `Api/GariKaagada.Api.Data/GariKaagada.Api.Data.csproj` to
      `BFF/GariKaagada.BFF/GariKaagada.BFF.csproj`, run `dotnet build GariKaagada.slnx` and
      confirm it fails, then revert the temporary reference and confirm
      `dotnet build GariKaagada.slnx` succeeds again — proves SC-002.

**Checkpoint**: Illegal cross-tier references are confirmed to fail at build time — User Story 2
is independently complete and demonstrable.

---

## Phase 5: User Story 3 - One shared definition for cross-boundary request/response types (Priority: P3)

**Goal**: Prove the Contracts → NSwag → generated-TypeScript pipeline works end-to-end with one
placeholder request/response pair.

**Independent Test**: Add `PingPayload`/`PingDto` to `GariKaagada.Contracts`, confirm an
equivalent TypeScript type is generated for `gari-kagada-client` without hand-authoring it
(per quickstart.md).

### Implementation for User Story 3

- [X] T028 [P] [US3] Create `PingPayload` record (`Message: string`) in
      `GariKaagada.Contracts/Ping/PingPayload.cs` (data-model.md).
- [X] T029 [P] [US3] Create `PingDto` record (`Message: string`, `ReceivedAtUtc: DateTime`) in
      `GariKaagada.Contracts/Ping/PingDto.cs` (data-model.md).
- [X] T030 [US3] Create `PingPayloadValidator` in
      `GariKaagada.Contracts/Ping/PingPayloadValidator.cs`, enforcing `Message` is required and
      ≤200 characters (depends on T028).
- [X] T031 [US3] Add a `POST /api/ping` endpoint to `GariKaagada.Api` accepting `PingPayload`
      and returning `PingDto` (contracts/health-and-ping-endpoints.md) (depends on T028, T029,
      T009).
- [X] T032 [US3] Add a `POST /api/ping` endpoint to `GariKaagada.BFF` that validates the request
      via `PingPayloadValidator`, forwards it to `GariKaagada.Api`'s `/api/ping` using the
      `HttpClient` registered in T023, and returns the resulting `PingDto` (depends on T030,
      T031, T023).
- [X] T033 [US3] Configure NSwag (`nswag.json` or an `NSwag.MSBuild` target) in
      `BFF/GariKaagada.BFF/` to generate a TypeScript client/types from `GariKaagada.BFF`'s
      OpenAPI spec into `gari-kagada-client/src/app/generated/` as part of the build pipeline,
      with generated files committed (constitution Principle XI) (depends on T032, T014).
- [X] T034 [US3] Run quickstart.md's "Validate User Story 3" steps: `POST` a valid payload
      (expect `200` + `PingDto`), `POST` an invalid payload missing `message` (expect `400`,
      proving `PingPayloadValidator` is enforced on the request path), confirm the generated
      TypeScript type exists in `gari-kagada-client/src/app/generated/`, and confirm deleting +
      regenerating it produces an identical result (SC-004).

**Checkpoint**: A developer can add one shared type in `GariKaagada.Contracts` and have it reach
the frontend automatically — User Story 3 is independently complete and demonstrable.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Repo-wide consistency checks that span all three user stories.

- [X] T035 [P] Regenerate `AGENTS.md` at the repository root to reflect the now-scaffolded
      project structure (flagged as a follow-up in the constitution's v2.2.0 Sync Impact
      Report).
- [X] T036 [P] Run `dotnet build GariKaagada.slnx` for the whole solution and confirm zero
      warnings (constitution Principle X's `TreatWarningsAsErrors`/`Nullable` mandate applies
      solution-wide, not just per project).
- [X] T037 Run the complete quickstart.md validation guide end-to-end, all three user stories
      plus the idempotent-restart check, as final sign-off that the scaffolding satisfies every
      success criterion in spec.md (SC-001 through SC-006).

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — start immediately.
- **Foundational (Phase 2)**: Depends on Setup (needs `Directory.Packages.props`/
  `Directory.Build.props`/`GariKaagada.slnx` to exist first) — BLOCKS all user stories.
- **User Stories (Phase 3–5)**: All depend on Foundational completion.
  - US1 (Phase 3) has no dependency on US2 or US3.
  - US2 (Phase 4) depends only on the project references Foundational already created — no
    dependency on US1 or US3, and can run in parallel with either.
  - US3 (Phase 5) depends on US1's `GariKaagada.BFF`↔`GariKaagada.Api` `HttpClient` wiring
    (T023) and the Angular project (T014) — practically sequenced after US1, though nothing in
    the spec forbids starting US3's Contracts-only tasks (T028–T030) earlier.
- **Polish (Phase 6)**: Depends on all three user stories being complete.

### User Story Dependencies

- **User Story 1 (P1)**: Can start immediately after Foundational. No dependency on US2/US3.
- **User Story 2 (P2)**: Can start immediately after Foundational. Independent of US1/US3 —
  it only validates Foundational's reference rules.
- **User Story 3 (P3)**: Can start immediately after Foundational for its Contracts-only tasks
  (T028–T030); its endpoint/NSwag tasks (T031–T033) need US1's T023 (the BFF→Api `HttpClient`)
  to exist first.

### Within Each User Story

- US1: container declarations (T017–T019) before project-resource wiring that `WaitFor`/
  `WaitForCompletion`s them (T020–T022); hub/HttpClient wiring (T023–T024) can run in parallel
  with the AppHost tasks since they're in a different project.
- US3: models (T028, T029) before the validator (T030) before the endpoints (T031, T032)
  before NSwag generation (T033).

### Parallel Opportunities

- All Setup tasks marked [P] (T002–T004) can run in parallel once T001 exists.
- Foundational tasks marked [P] (T005–T011, T014) can run in parallel — each creates an
  independent project; T008/T009 depend on T006/T007, T010/T011 depend on T006, but the two
  tiers (Api-side vs BFF-side) don't depend on each other and can proceed in parallel.
- Once Foundational (Phase 2) completes, US1 and US2 can be worked in parallel by different
  people; US3's Contracts-only tasks (T028–T030) can also start immediately.
- Within US3, T028 and T029 (the two record types) can run in parallel.
- Polish tasks T035 and T036 can run in parallel; T037 depends on everything else being done.

---

## Parallel Example: Phase 2 (Foundational)

```bash
# Launch independent project-creation tasks together:
Task: "Create GariKaagada.ServiceDefaults class library in GariKaagada.ServiceDefaults/"
Task: "Create GariKaagada.Contracts class library in GariKaagada.Contracts/"
Task: "Scaffold gari-kagada-client/ via ng new + ng add @angular/material"

# Once GariKaagada.Contracts exists, the Api-tier and BFF-tier projects can each proceed in parallel:
Task: "Create GariKaagada.Api.Data in Api/GariKaagada.Api.Data/"
Task: "Create GariKaagada.BFF.Business in BFF/GariKaagada.BFF.Business/"
```

## Parallel Example: Phase 5 (User Story 3)

```bash
Task: "Create PingPayload record in GariKaagada.Contracts/Ping/PingPayload.cs"
Task: "Create PingDto record in GariKaagada.Contracts/Ping/PingDto.cs"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup.
2. Complete Phase 2: Foundational (CRITICAL — blocks all stories).
3. Complete Phase 3: User Story 1 — the full stack starts, healthy, idempotently.
4. **STOP and VALIDATE**: run quickstart.md's User Story 1 + idempotency sections.
5. This alone is a demonstrable MVP: "clone the repo, run one command, the whole
   constitution-mandated stack comes up healthy."

### Incremental Delivery

1. Setup + Foundational → every project exists and compiles.
2. Add User Story 1 → the stack runs end-to-end → demo the Aspire dashboard.
3. Add User Story 2 → demo the build failing on an illegal reference, then succeeding once
   reverted.
4. Add User Story 3 → demo one Contracts type reaching the frontend automatically.
5. Polish → regenerate `AGENTS.md`, confirm a clean solution-wide build, full quickstart sign-off.

### Parallel Team Strategy

With multiple developers, after Foundational is done:
- Developer A: User Story 1 (AppHost + container wiring).
- Developer B: User Story 2 (validation-only — low effort, can also help elsewhere after).
- Developer C: User Story 3 (Contracts + NSwag pipeline), starting with T028–T030 immediately
  and picking up T031–T033 once Developer A's T023 lands.

---

## Notes

- No test tasks are included anywhere in this file — constitution Principle XV prohibits test
  projects/files unconditionally, and that was not overridden for this feature.
- [P] tasks touch different files with no incomplete-task dependency between them.
- Several tasks (T017–T019, T022) reference "verify at implementation time" items flagged in
  research.md (exact SigNoz container topology, exact Aspire JS-hosting API name, current image
  major versions) — resolve those against official docs immediately before implementing the
  task, per constitution Principle X.
- Commit after each task or logical group; stop at either checkpoint above to validate a story
  independently before continuing.
