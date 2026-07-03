# Implementation Plan: Project Scaffolding

**Branch**: `001-project-scaffolding` | **Date**: 2026-07-03 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/001-project-scaffolding/spec.md`

## Summary

Stand up the empty, constitution-mandated solution/project skeleton for GariKaagada — every
project named in Principle VII (Layered .NET Project Architecture), wired together through one
.NET Aspire AppHost, with Central Package Management, the Angular client orchestrated alongside
the .NET services, and the Contracts→NSwag→TypeScript pipeline proven end-to-end with a single
placeholder request/response pair. No product feature logic (auth, kagada, distribution) is
implemented — only enough per project to prove it compiles, starts, and is correctly wired.

## Technical Context

**Language/Version**: C# (.NET version matching the installed Aspire 13.4.6 SDK — verify exact
TFM against `dotnet --version`/Aspire's own compatibility table at implementation time, per
Principle X's "official docs over assumption" rule, rather than hardcoding a version number
here) for all backend projects; TypeScript + Angular (latest stable/LTS — Angular 22 is current
stable as of this writing; **verify exact version via `ng version`/npm at implementation time**,
since this plan should not freeze a version number that may have shipped a newer release by
then) for the frontend.

**Primary Dependencies**:
- Backend: ASP.NET Core Web API, EF Core + Npgsql (PostgreSQL provider), FluentValidation,
  NSwag (`NSwag.AspNetCore`/`NSwag.MSBuild`), `Microsoft.AspNetCore.SignalR` (built into ASP.NET
  Core, no extra package), the Aspire `ServiceDefaults` template's own dependencies
  (`OpenTelemetry.*`, `Microsoft.Extensions.ServiceDiscovery`, `Microsoft.Extensions.Http.Resilience`),
  and the Aspire hosting package for the frontend integration (`Aspire.Hosting.JavaScript`'s
  `AddJavaScriptApp`, Aspire 13's unified Node/npm-app hosting API — **verify this is the
  current package/method name against `aspire.dev`/`learn.microsoft.com` docs at implementation
  time**; the older `Aspire.Hosting.NodeJs`'s `AddNpmApp` is the documented fallback if
  `AddJavaScriptApp` isn't available in the installed Aspire version).
- Frontend: Angular, Angular Material (installed via `ng add @angular/material`, M3 theme
  scaffolded via its `m3-theme` schematic), NgRx Signal Store, `@microsoft/signalr`,
  NSwag-generated TypeScript client (no hand-written API types).

**Storage**: Self-hosted PostgreSQL (Principle II, XIII) — for this feature, an EF Core
`DbContext` with **zero `DbSet`s** exists solely to prove `GariKaagada.Api.Data` compiles and
`GariKaagada.MigrationWorker` can run an (empty) migration; no entities are defined yet.

**Testing**: N/A — Principle XV (No Test Projects) prohibits test projects/files entirely, in
this feature and every other.

**Target Platform**: Self-hosted Linux via Podman containers (PostgreSQL, Keycloak, SigNoz
stack) for the backend/infra tier; any modern evergreen browser for `gari-kagada-client`.

**Project Type**: Web application — multiple .NET services (BFF + internal API, each split into
Web/Business/Data tiers per Principle VII) plus one Angular frontend, all orchestrated by one
Aspire AppHost. This does not match either generic "Option 1/2" shape in the template below; see
Project Structure for the real, constitution-derived layout.

**Performance Goals**: None specific to this feature beyond spec.md's SC-001 (full stack healthy
within 5 minutes of a fresh clone, one start command).

**Constraints**: Every constraint in constitution Technical Constraints applies to however much
of each project this feature creates (see Constitution Check). No business/domain logic, no
entities beyond the empty `DbContext`, no real UI screens, no real hub methods — only enough
per Principle VII project to prove it exists, compiles, and is wired correctly (spec.md FR-007).
Backing containers MUST use major-version-pinned image tags and a persistent container lifetime
+ data volume so a killed/partial `aspire run` can always be re-run without manual cleanup
(spec.md FR-009, SC-006 — see research.md).

**Scale/Scope**: 9 .NET projects + 1 Angular project + 3 self-hosted backing services
(PostgreSQL, Keycloak, SigNoz stack), one Aspire AppHost, one solution.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

This feature only builds structure; most principles' *behavioral* rules have nothing to check
yet (no business logic exists to violate them). Each principle below is marked PASS (fully
satisfied by scaffolding), STRUCTURAL (the structural/wiring portion is satisfied now; the
behavioral portion is explicitly out of scope until a feature adds real logic), or N/A (nothing
in scaffolding triggers this principle at all).

| Principle | Status | Notes |
|---|---|---|
| I. Zero-PII, Password-Only Auth | STRUCTURAL | Keycloak container declared & reachable via Aspire (Principle II overlap); realm/client config, protocol mapper, and BFF token validation are auth-feature work, not scaffolding. |
| II. Self-Hosted Sovereignty & IaC | PASS | PostgreSQL, Keycloak, SigNoz stack all declared as Podman containers in `GariKaagada.AppHost`, pinned to major-version image tags and given a persistent container lifetime + data volume (clarification session 2026-07-03, spec.md FR-009/SC-006); no managed cloud service used. |
| III. Angular Material + Peacock Design | STRUCTURAL | Angular Material installed, M3 theme file scaffolded; real peacock-derived token values and any actual screens/components are feature work. |
| IV. Reactive-First Architecture | STRUCTURAL | NgRx Signal Store package installed; no stores yet (nothing to manage state for). |
| V. Real-Time Delivery via SignalR | STRUCTURAL | BFF maps one empty `Hub` class (proves the hub hosting mechanism works, satisfies Principle VI's "hosts the SignalR hub"); no real hub methods/events (feature work). `@microsoft/signalr` installed in the frontend. |
| VI. BFF Architecture | STRUCTURAL | BFF and Api are separate projects; BFF has an `HttpClient` registered against Api via Aspire service discovery (`WithReference`) proving the internal transport is wired; the frontend's health is verified via a real HTTP request to its dev server (clarification session 2026-07-03, spec.md FR-005), not just process-running state; no real proxied endpoint yet. |
| VII. Layered .NET Project Architecture | PASS | This is what the feature delivers: every named project, every reference rule, Central Package Management, solution-folder layout. |
| VIII. Kagada Distribution Algorithm Integrity | N/A | No business logic in scope; `GariKaagada.Api.Business` exists as an empty class library ready to receive it. |
| IX. Recipient Consent & Control | N/A | No business logic in scope. |
| X. Code Quality & Maintainability First | PASS | `<TreatWarningsAsErrors>`/`<Nullable>enable</Nullable>` set solution-wide via `Directory.Build.props`; Angular `strict`/`strictTemplates` set in the scaffolded `tsconfig.json`; every technology choice in this plan is flagged for official-doc verification per the same principle's newest sub-rule. |
| XI. Contract-Driven Validation & TS Generation | STRUCTURAL | The full pipeline (FluentValidation validator in Contracts → NSwag → generated TS) is proven end-to-end with one placeholder Payload/Dto pair (User Story 3); real product Payloads/Dtos are feature work. |
| XII. Observability & Structured Logging | STRUCTURAL | `ServiceDefaults` wires OTel export to SigNoz for every runnable service; SigNoz stack containers declared. No real structured log call sites exist yet (no business logic to log). |
| XIII. EF Core Conventions | DEFERRED | `AuditableEntity` base class and the audit `SaveChangesInterceptor` are deferred to the first feature that adds a real entity — creating them with zero entities to prove them against would be speculative, not scaffolding. Documented as an explicit assumption below. |
| XIV. Interface-Driven Design | N/A | No non-trivial classes exist yet in `.Business`/`.Data` projects to require an interface. |
| XV. No Test Projects | PASS | No test project, test file, or test library is created by this feature. |

**Gate result**: PASS. No unjustified violations — the DEFERRED item (XIII) is a scoping
decision consistent with spec.md's FR-007 ("no domain/business logic... beyond the minimum
needed to prove the project builds and starts"), not a violation of the principle itself; it is
recorded in Complexity Tracking for visibility even though it isn't a complexity addition.

## Project Structure

### Documentation (this feature)

```text
specs/001-project-scaffolding/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md         # Phase 1 output
├── quickstart.md         # Phase 1 output
├── contracts/            # Phase 1 output
└── tasks.md              # Phase 2 output (/speckit-tasks — not created by /speckit-plan)
```

### Source Code (repository root)

```text
GariKaagada.slnx                  # .slnx solution file (default format for current-gen .NET
                                   # SDK tooling); falls back to a classic .sln if the installed
                                   # SDK predates .slnx support — verify at implementation time.
Directory.Packages.props          # Central Package Management — every NuGet version pinned here.
Directory.Build.props             # Solution-wide MSBuild props: TreatWarningsAsErrors, Nullable.
nuget.config                       # (existing, unchanged)
aspire.config.json                 # updated: appHost.path now points at the AppHost project,
                                   # not the standalone apphost.cs file (see research.md).
README.md                          # updated with Podman/Aspire prerequisites for this scaffold.
AGENTS.md                          # regenerated to reflect the scaffolded structure once real.

GariKaagada.AppHost/
├── GariKaagada.AppHost.csproj
└── AppHost.cs                     # or Program.cs — matches whatever the installed Aspire
                                    # project template emits; declares every project reference
                                    # and container (PostgreSQL, Keycloak, SigNoz stack,
                                    # gari-kagada-client via AddJavaScriptApp/AddNpmApp).
                                    # Backing containers: major-version image tags +
                                    # WithLifetime(ContainerLifetime.Persistent) + a data
                                    # volume each, so restarts are idempotent (SC-006).

GariKaagada.ServiceDefaults/
├── GariKaagada.ServiceDefaults.csproj
└── Extensions.cs                  # AddServiceDefaults/ConfigureOpenTelemetry/
                                    # AddDefaultHealthChecks/MapDefaultEndpoints, per the
                                    # official Aspire ServiceDefaults template.

GariKaagada.MigrationWorker/
├── GariKaagada.MigrationWorker.csproj
└── Program.cs                     # runs EF Core migrations for the (currently empty) DbContext
                                    # at startup; references Api.Data + ServiceDefaults.

BFF/                                # solution folder
├── GariKaagada.BFF/
│   ├── GariKaagada.BFF.csproj
│   ├── Program.cs                 # maps default endpoints, the empty SignalR hub, and an
│   │                               # HttpClient registered against Api via service discovery.
│   ├── Hubs/AppHub.cs              # empty Hub — no methods yet (Principle V, structural only).
│   └── nswag.json                 # (or an NSwag MSBuild target in the .csproj) generating
│                                   # TypeScript into gari-kagada-client from this project's
│                                   # OpenAPI spec.
└── GariKaagada.BFF.Business/
    └── GariKaagada.BFF.Business.csproj   # empty class library.

Api/                                # solution folder
├── GariKaagada.Api/
│   ├── GariKaagada.Api.csproj
│   └── Program.cs                 # maps default endpoints only.
├── GariKaagada.Api.Business/
│   └── GariKaagada.Api.Business.csproj   # empty class library.
└── GariKaagada.Api.Data/
    ├── GariKaagada.Api.Data.csproj
    └── GariKaagadaDbContext.cs     # EF Core DbContext, zero DbSets (Principle XIII deferred).

GariKaagada.Contracts/
├── GariKaagada.Contracts.csproj
└── Ping/
    ├── PingPayload.cs              # placeholder Payload record (User Story 3 proof).
    ├── PingDto.cs                  # placeholder Dto record (User Story 3 proof).
    └── PingPayloadValidator.cs     # minimal FluentValidation validator for PingPayload.

gari-kagada-client/                 # Angular CLI project; not part of the .NET solution.
├── angular.json / package.json / tsconfig.json (strict: true) / src/...
└── src/app/generated/              # NSwag-generated TypeScript client output (committed).
```

**Structure Decision**: One top-level `.slnx` solution with `BFF` and `Api` solution folders
(Principle VII), a project-based Aspire AppHost (see research.md for why this replaces the
repo's current single-file `apphost.cs`), Central Package Management via
`Directory.Packages.props`, and `gari-kagada-client` orchestrated as an Aspire resource rather
than run by hand. This is the literal structure Principle VII already mandates — this feature
has no structural discretion of its own beyond resolving the "verify at implementation time"
items flagged above.

## Complexity Tracking

> No unjustified Constitution Check violations — nothing below is a *violation*; it records one
> deliberate scope deferral for traceability.

| Item | Why Deferred | Simpler Alternative Rejected Because |
|---|---|---|
| `AuditableEntity` base class + audit `SaveChangesInterceptor` (Principle XIII) | No entity exists yet for it to apply to; scaffolding it now would be unverified, speculative code with nothing to prove it against. | Building it now "to save time later" was rejected — Principle X ("no shortcuts... best practices over... good enough for now") requires this to be built and verified against a real entity, not guessed at in the abstract. |
