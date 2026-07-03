# Phase 0 Research: Project Scaffolding

Per constitution Principle X ("official documentation over subjective preference"), each
decision below cites what was actually checked during this planning session, not assumption.
Items marked **verify at implementation time** are facts that are known to drift across tool
releases and must be re-checked against current official docs immediately before the
corresponding task is implemented — this plan intentionally does not freeze a guessed value.

## Decision: Convert the AppHost from a single-file `apphost.cs` to a project-based `GariKaagada.AppHost`

**Decision**: Replace the repo's current lightweight, file-based AppHost (`apphost.cs` with a
`#:sdk Aspire.AppHost.Sdk@13.4.6` directive) with a traditional Aspire AppHost **project**
(`GariKaagada.AppHost/GariKaagada.AppHost.csproj`), included in the solution.

**Rationale**: Constitution Principle VII names `GariKaagada.AppHost` as a project inside "one
top-level solution" with solution folders and Central Package Management spanning every .NET
project. Aspire's single-file AppHost mode exists for minimal, dependency-light orchestration of
a handful of resources declared inline; it does not naturally participate in a multi-project
solution with `Directory.Packages.props`-governed package versions or `.slnx` solution-folder
grouping. A project-based AppHost is the form every other constitution-mandated project already
takes, and is what the official multi-project Aspire templates (`aspire new`/`dotnet new
aspire-apphost`) produce.

**Alternatives considered**: Keep the file-based `apphost.cs` — rejected because it doesn't
compose with Central Package Management or solution-folder structure, and none of the official
"add a project to an Aspire solution" guidance targets the single-file mode.

**Follow-up (verify at implementation time)**: Confirm whether the currently-installed Aspire
CLI/SDK names the entry-point file `AppHost.cs` or `Program.cs` for a project-based AppHost —
this has changed across Aspire releases; use whatever `aspire new`/the installed project
template actually emits rather than assuming.

## Decision: `.slnx` as the primary solution file format

**Decision**: Use `GariKaagada.slnx` (the XML-based solution format) as the solution file.

**Rationale**: Confirmed via Microsoft's own `dotnet sln` docs and the .NET blog announcement:
`.slnx` is supported by the `dotnet` CLI starting SDK 9.0.200, is the **default** format
`dotnet new sln` produces starting in .NET 10, and is supported by Visual Studio 2022 17.13+,
Rider 2024.3+, and MSBuild 17.12+. It also has documented support for referencing
`Directory.Build.props`/`Directory.Packages.props` as Solution Items, which is directly relevant
here.
Sources: [dotnet sln command docs](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-sln), [Introducing SLNX support in the .NET CLI](https://devblogs.microsoft.com/dotnet/introducing-slnx-support-dotnet-cli/)

**Alternatives considered**: Classic `.sln` — still fully supported and is the safe fallback if
the installed SDK predates 9.0.200, but offers no advantage here given `.slnx` is confirmed
current-default tooling.

**Follow-up (verify at implementation time)**: Run `dotnet --version` before creating the
solution; if the installed SDK is older than 9.0.200, use classic `.sln` instead and note the
deviation in a follow-up amendment note (not a constitution violation — the constitution names
the AppHost/projects, not the solution file format).

## Decision: Central Package Management + `Directory.Build.props` split

**Decision**: Two separate solution-root files, each with one job:
- `Directory.Packages.props` — `<ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>`
  and every `<PackageVersion>` pin (constitution-mandated, Principle VII).
- `Directory.Build.props` — shared MSBuild properties every project needs identically:
  `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>`, `<Nullable>enable</Nullable>`
  (constitution Principle X), and the implicit-usings/target-framework settings common to every
  project.

**Rationale**: These are two distinct, official MSBuild mechanisms with non-overlapping jobs —
Central Package Management governs *package versions*; `Directory.Build.props` governs *build
properties*. The constitution only names the former explicitly (Principle VII), but Principle
X's zero-warning-build mandate applies solution-wide, and `Directory.Build.props` is the
standard, official way to apply that once instead of duplicating `<TreatWarningsAsErrors>`/
`<Nullable>` into all nine `.csproj` files.
Source: [dotnet sln / SLNX solution-items support for both files](https://devblogs.microsoft.com/dotnet/introducing-slnx-support-dotnet-cli/)

## Decision: `GariKaagada.ServiceDefaults` matches the official Aspire ServiceDefaults template shape

**Decision**: A single `Extensions.cs` exposing `AddServiceDefaults()`, which internally calls
`ConfigureOpenTelemetry()`, `AddDefaultHealthChecks()`, and is paired with
`MapDefaultEndpoints()` (mapping `/health` and `/alive`) called from each service's `Program.cs`.

**Rationale**: Confirmed directly against Aspire's own docs (`aspire.dev/get-started/csharp-service-defaults`)
and Microsoft Learn: this is the literal, official shape of the template Aspire itself
generates for every new service project, including the `FrameworkReference` on
`Microsoft.AspNetCore.App`, the OTel SDK + ASP.NET/HttpClient/Runtime instrumentation packages,
and the OTLP exporter wired to whatever collector endpoint is configured (SigNoz, per Principle
XII, rather than the Aspire Dashboard's own OTLP endpoint used in Aspire's local-dev default).
Source: [Aspire C# ServiceDefaults project: OTel and resilience](https://aspire.dev/get-started/csharp-service-defaults/)

**Follow-up (verify at implementation time)**: Confirm the exact OTLP endpoint/env-var wiring
needed to point `ConfigureOpenTelemetry()` at the self-hosted SigNoz stack rather than the
Aspire Dashboard's own collector — this is an AppHost `WithEnvironment(...)` concern for whoever
implements the SigNoz container wiring, not something to guess here.

## Decision: Frontend hosting via Aspire's JavaScript/Node integration

**Decision**: Orchestrate `gari-kagada-client` from `GariKaagada.AppHost` using Aspire 13's
JavaScript-app hosting integration (`Aspire.Hosting.JavaScript`'s `AddJavaScriptApp`, the
unified API Aspire 13 introduced for npm-script-based frontends including Angular), referencing
`GariKaagada.BFF` via `WithReference` so the frontend gets the BFF's address through Aspire
service discovery rather than a hardcoded URL.

**Rationale**: Confirmed via Microsoft's official Aspire blog/docs: Aspire 13 brought
JavaScript/TypeScript in as first-class citizens via `Aspire.Hosting.JavaScript` and
`AddJavaScriptApp()`, explicitly called out as working for React, Angular, Vue, Express, and
Next.js projects driven by npm scripts. The older, still-documented `Aspire.Hosting.NodeJs`
package's `AddNpmApp` is the confirmed fallback if the installed Aspire 13.4.6 SDK's JavaScript
package isn't present for some reason.
Sources: [Aspire for JavaScript developers](https://devblogs.microsoft.com/aspire/aspire-for-javascript-developers/), [Orchestrate Node.js apps in .NET Aspire](https://learn.microsoft.com/en-us/dotnet/aspire/get-started/build-aspire-apps-with-nodejs), [AddNpmApp API reference](https://learn.microsoft.com/en-us/dotnet/api/aspire.hosting.nodeapphostingextension.addnpmapp)

**Follow-up (verify at implementation time)**: Confirm `Aspire.Hosting.JavaScript` is actually
present/resolvable for Aspire 13.4.6 specifically (the search results confirm it for "Aspire
13" generally, sourced from a blog post rather than a versioned API reference) before writing
the AppHost code; fall back to `Aspire.Hosting.NodeJs`'s `AddNpmApp` if not.

## Decision: Angular scaffold — version, standalone components, Material M3 theme

**Decision**: Scaffold `gari-kagada-client` with `ng new` using standalone components (Angular's
default since v17 — no NgModules), `"strict": true` in `tsconfig.json`, then `ng add
@angular/material` (selecting the "Custom" theme option so no pre-built CSS is hardcoded, per
constitution Principle III's ban on non-token colors), and generate the M3 theme via Angular
Material's `ng generate @angular/material:m3-theme` schematic.

**Rationale**: Confirmed standalone-by-default since Angular v17; Angular 22 is the current
stable release referenced by industry sources as of this session. Angular Material's M3 theming
schematic (`m3-theme`) is the officially documented path to a from-scratch M3 theme, matching
Principle III's "M3 tokens only, no hard-coded hex values" requirement — selecting "Custom"
during `ng add` is what avoids Angular Material silently writing a pre-built, non-token theme
file.
Sources: [Angular standalone migration guide](https://angular.dev/reference/migrations/standalone), [Angular Material M3 theming guide](https://material.angular.dev/guide/theming)

**Follow-up (verify at implementation time)**: Re-confirm the exact current Angular version and
whether Angular Material's M3 support has moved out of `@angular/material-experimental` into
`@angular/material` proper by the time this is implemented (it was experimental as of Angular
Material v17.2; this plan assumes it has since stabilized given the current date, but the exact
import path must be checked against the installed package version, not assumed).

## Decision: Backing containers pinned to major-version image tags

**Decision**: PostgreSQL, Keycloak, and every SigNoz-stack container are declared with a
major-version-only image tag (e.g., `postgres:16`, not `postgres:16.4` or `postgres:latest`).

**Rationale**: Resolved directly by clarification session 2026-07-03 (spec.md FR-009): major-only
pinning gets automatic patch-level fixes without the non-reproducibility risk of a floating
`latest` tag, and without the maintenance overhead of bumping an exact patch version by hand for
every image on every update.

**Follow-up (verify at implementation time)**: Confirm the current major version of each image
(PostgreSQL, Keycloak, and whichever specific SigNoz-stack images the SigNoz self-host docs
specify — flagged as unresolved topology in the earlier SigNoz research) against each project's
own release notes/Docker Hub tags page before pinning, rather than assuming a version number.

## Decision: Idempotent restart via Aspire's persistent container lifetime

**Decision**: Every backing-service container resource (PostgreSQL, Keycloak, SigNoz stack) is
declared with `.WithLifetime(ContainerLifetime.Persistent)` combined with a named data volume
(`.WithDataVolume(...)`/`.WithVolume(...)`). The AppHost projects (BFF, Api, MigrationWorker,
frontend) keep Aspire's default (non-persistent) lifetime, since re-creating them on every
`aspire run` is fine — they hold no state of their own.

**Rationale**: Confirmed via Microsoft's own Aspire docs: Aspire hashes a container resource's
configuration and reuses the existing container/volume across AppHost runs when the hash
matches, rather than recreating it — but only when a persistent lifetime is requested; the
default lifetime removes and recreates containers on every run and disposes them when the
AppHost stops. `WithLifetime(ContainerLifetime.Persistent)` (or the container-specific
`WithLifetime(ContainerLifetime.Persistent)` API) plus a named data volume is the documented
mechanism that satisfies spec.md SC-006 (re-running the start command after a partial failure
succeeds without manual cleanup) — a fresh `aspire run` after a killed/partial previous attempt
reuses the same container/volume rather than colliding with an orphaned one from before.
Source: [Persistent container lifetimes in Aspire](https://learn.microsoft.com/en-us/dotnet/aspire/app-host/persistent-containers), [Aspire Quick Tip - Managing Container & Data Lifetime](https://devblogs.microsoft.com/dotnet/dotnet-aspire-container-lifetime/), [Persist Aspire project data using volumes or bind mounts](https://learn.microsoft.com/en-us/dotnet/aspire/fundamentals/persist-data-volumes)

**Alternatives considered**: Leaving backing containers at Aspire's default (non-persistent)
lifetime — rejected because it directly contradicts SC-006; a killed mid-startup attempt would
otherwise leave no reusable container, and the next `aspire run` would need to fully
reprovision (slower, and the exact scenario SC-006 was written to rule out ambiguity on).

**Follow-up (verify at implementation time)**: Confirm the exact current API name/namespace for
`ContainerLifetime.Persistent` and `WithDataVolume` against the installed Aspire 13.4.6 SDK's
IntelliSense/API reference — these are Aspire Hosting APIs that have moved namespaces across
major versions historically.

## Decision: `PingPayload`/`PingDto` as the one placeholder Contracts pair proving User Story 3

**Decision**: Add exactly one placeholder request/response pair (`PingPayload` → `PingDto`) to
`GariKaagada.Contracts`, with a minimal `FluentValidation` validator, solely to exercise the
full Contracts → NSwag → generated-TypeScript pipeline end-to-end (spec.md User Story 3 /
FR-006 / SC-004). This is not product feature logic — it is the smallest possible proof that
the mechanism spec.md requires actually works, and is called out explicitly in spec.md's
Acceptance Scenarios for User Story 3.

**Rationale**: Without at least one real type flowing through the pipeline, User Story 3's
acceptance scenario ("Given a new request or response type added... an equivalent type is
available to frontend code") cannot be demonstrated at all — an empty Contracts project proves
nothing. A single throwaway pair is the minimal artifact that satisfies the spec without
introducing any product-domain meaning (no `Kagada`, `User`, or other domain nouns appear).
