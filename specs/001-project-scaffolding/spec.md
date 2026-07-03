# Feature Specification: Project Scaffolding

**Feature Branch**: `001-project-scaffolding` (created on `main`; no branch-creation hook is
registered in this repo, so no separate git branch was checked out)

**Created**: 2026-07-03

**Status**: Draft

**Input**: User description: "Project scaffolding: create the empty solution/project structure
mandated by the constitution (.specify/memory/constitution.md, Principle VII) —
GariKaagada.AppHost, GariKaagada.ServiceDefaults, GariKaagada.MigrationWorker, the BFF tier
(GariKaagada.BFF, GariKaagada.BFF.Business), the Api tier (GariKaagada.Api,
GariKaagada.Api.Business, GariKaagada.Api.Data), GariKaagada.Contracts, and the
gari-kagada-client Angular project, all wired together through the Aspire AppHost. Do not
implement any actual feature logic, entities, endpoints, or business rules — only the empty
project skeletons, project references, solution structure, Central Package Management file, and
Aspire AppHost registration so the whole stack builds and starts."

## Clarifications

### Session 2026-07-03

- Q: Should the backing container images (PostgreSQL, Keycloak, SigNoz stack) be version-pinned
  or allowed to float? → A: Pin major version only (e.g., `postgres:16`) — auto-picks up
  patches, not major upgrades.
- Q: If the stack fails to start partway through, must re-running the start command succeed
  without manual cleanup? → A: Yes — fully idempotent; re-running always succeeds without
  manual cleanup, regardless of any prior partial failure.
- Q: For the Angular frontend resource, what should "healthy" mean for User Story 1's
  acceptance test? → A: An HTTP request to the frontend's dev server succeeds (a real
  reachability check), not merely the process being reported as running.

## User Scenarios & Testing *(mandatory)*

<!--
  This feature's "user" is the developer working on GariKaagada, not an end user of the
  letter-writing product itself — it establishes the empty, correctly-wired project skeleton
  that every future product feature will be built inside. No product-facing behavior is in
  scope here.
-->

### User Story 1 - Start the full local stack with one command (Priority: P1)

A developer clones the repository and starts the orchestration project. Every declared service
and backing container (database, identity provider, observability backend, frontend) comes up
as part of one application graph, without any manual per-service startup steps.

**Why this priority**: Nothing else can be built or demonstrated until the whole stack reliably
starts locally. This is the foundational proof that the scaffolding is wired correctly.

**Independent Test**: Can be fully tested by cloning the repo, starting the orchestration
project, and observing every declared resource reach a healthy/running state — delivers the
value of a working local environment before any feature code exists.

**Acceptance Scenarios**:

1. **Given** a fresh clone of the repository with prerequisite tooling installed, **When** the
   developer starts the orchestration project, **Then** every declared project and backing
   service reaches a healthy/running state without additional manual setup — for the frontend
   client specifically, "healthy" means an HTTP request to its dev server succeeds, not merely
   the process being reported as running.
2. **Given** the stack is running, **When** the developer stops the orchestration project,
   **Then** every service and container it started stops cleanly.

---

### User Story 2 - Illegal cross-tier references fail at build time (Priority: P2)

A developer adds a reference from one project to another that violates the mandated layering
(for example, a data-access project referencing a web API project). The build fails immediately
rather than the mistake surfacing later in code review or at runtime.

**Why this priority**: The whole point of the layered structure is that architectural mistakes
become compiler errors. If illegal references silently compile, the scaffolding has not actually
delivered its core value.

**Independent Test**: Can be fully tested by attempting to add a reference that violates the
mandated project layering and confirming the build fails, independent of any other feature work.

**Acceptance Scenarios**:

1. **Given** the scaffolded solution, **When** a project reference is added from a lower-tier
   project to a project above it in the layering, **Then** the build fails.
2. **Given** the scaffolded solution, **When** a project reference follows the mandated
   layering, **Then** the build succeeds.

---

### User Story 3 - One shared definition for cross-boundary request/response types (Priority: P3)

A developer needs a new type that both the backend and the frontend must agree on the shape of.
They define it once in the shared contracts location, and an equivalent, correctly-typed version
becomes available to the frontend automatically — never hand-copied.

**Why this priority**: This prevents an entire class of frontend/backend disagreement bugs, but
it only matters once the stack from User Story 1 is running and the layering from User Story 2
is enforced, so it is the third priority.

**Independent Test**: Can be fully tested by adding a placeholder request/response type to the
shared contracts project and confirming an equivalent type becomes available to the frontend
project without hand-authoring it there.

**Acceptance Scenarios**:

1. **Given** a new request or response type added to the shared contracts project, **When** the
   frontend build/generation step runs, **Then** an equivalent type is available to frontend
   code without manual duplication.

---

### Edge Cases

- What happens when a required backing service (database, identity provider, observability
  backend) fails to start? The orchestration project MUST report which specific service failed
  and why, rather than the whole stack hanging or failing silently. After such a failure, simply
  re-running the start command MUST succeed without any manual cleanup (removing containers or
  volumes by hand) — a partial failure MUST NOT leave state that blocks a subsequent attempt.
- What happens when the Central Package Management file is missing a version for a package a
  project needs? The build MUST fail with a clear, specific error identifying the missing
  package/version, not an inconsistent or silently-resolved version.
- What happens when the frontend project's own dependencies (its package manager packages) are
  not yet installed on a fresh clone? Starting the stack MUST surface a clear, actionable error
  for that specific project rather than an unexplained failure across the whole graph.
- What happens if someone attempts to add business/domain logic directly into this scaffolding
  work? Out of scope — this feature intentionally contains no feature logic, entities,
  endpoints, or business rules; any such code does not belong to this feature.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The solution MUST provide a distinct, empty (no business logic) project for every
  architectural tier defined in the project constitution: orchestration, shared service
  defaults, migration runner, BFF, BFF business logic, API, API business logic, API data access,
  shared contracts, and the frontend client.
- **FR-002**: Each project MUST be restricted to referencing only projects in its own tier or the
  tier(s) beneath it, such that an attempt to reference a project in a tier above it fails at
  build time.
- **FR-003**: The solution MUST provide one centralized location for pinning every third-party
  package version used across all backend projects; individual projects MUST NOT declare their
  own version for a package already pinned there.
- **FR-004**: The orchestration project MUST declare every other project and every backing
  service (database, identity provider, observability backend) as part of a single runnable
  application graph, such that starting the orchestration project starts the entire local stack.
- **FR-005**: The frontend project MUST be declared as part of the same runnable application
  graph as the backend projects, so it starts, stops, and is discoverable alongside them rather
  than being run separately by hand. Its health MUST be verifiable via an HTTP request to its
  dev server succeeding — not merely the process being reported as running.
- **FR-006**: The shared contracts project MUST provide a location for cross-boundary request
  and response type definitions, and the frontend project MUST receive an equivalent,
  automatically kept-in-sync version of those types rather than a hand-maintained copy.
- **FR-007**: None of the scaffolded projects MUST contain domain/business logic, data models,
  endpoints, or UI screens beyond the minimum needed to prove the project builds and starts
  (e.g., a default health/status response) — actual product feature behavior is explicitly out
  of scope for this work.
- **FR-008**: The scaffolded structure MUST NOT require any manual, undocumented setup step to
  run locally beyond installing the prerequisite tooling itself (runtime, container engine,
  package managers).
- **FR-009**: Every backing service container image (database, identity provider, observability
  backend) MUST be pinned to a specific major version (e.g., `postgres:16`), never a floating
  `latest` tag, so a fresh clone reproducibly resolves to a compatible image line while still
  picking up patch-level updates automatically.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A developer can go from a fresh clone to every declared service and container
  reporting a healthy/running state in under 5 minutes, using a single start command.
- **SC-002**: 100% of project references that violate the mandated layering are rejected at
  build time — zero layering violations are possible to introduce that only surface at runtime
  or in review.
- **SC-003**: Zero manual, undocumented setup steps are required to reach a fully running local
  stack beyond installing prerequisite tooling.
- **SC-004**: A developer introducing a new shared request/response type needs to author it in
  exactly one place for an equivalent, matching type to become available to frontend code.
- **SC-005**: Stopping the orchestration project leaves zero orphaned running services or
  containers behind.
- **SC-006**: After any partial startup failure, re-running the single start command succeeds
  without manual cleanup, on every attempt.

## Assumptions

- The concrete technology choices referenced throughout this spec (the orchestration framework,
  container runtime, database, identity provider, observability backend, and frontend
  framework) are already fixed by the ratified project constitution and are treated here as
  constraints, not open decisions.
- "Empty project" means each project exposes only the minimum needed to prove it builds and
  starts (e.g., a default health-check response or a default landing page) — not any real
  product capability.
- Developer machine prerequisites (a runtime SDK, a container engine, a frontend package
  manager) are assumed already installed per the constitution's self-hosting principle; this
  feature does not cover installing them.
- No CI/CD pipeline configuration is in scope — only the local, orchestrated project structure
  and its ability to build and start.
- No actual database schema, identity realm configuration, or observability dashboard content is
  in scope — only that these backing services are declared and reachable as part of the local
  stack.
