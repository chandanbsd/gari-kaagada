# Quickstart: Validating the Project Scaffolding

This validates the scaffolding delivers what spec.md promises — nothing here exercises product
features, since none exist yet.

## Prerequisites

- .NET SDK matching the Aspire 13.4.6 AppHost's required TFM (verify with `dotnet --version`).
- Podman installed and `ASPIRE_CONTAINER_RUNTIME=podman` exported (constitution Principle II).
- Node.js + npm (for `gari-kagada-client` and NSwag's TypeScript generation step).
- Run `aspire doctor` first — confirm it reports Podman as the active container runtime before
  proceeding to any step below.

## Validate User Story 1 — one command starts the full stack

1. From repo root: `aspire run` (or `dotnet run` from `GariKaagada.AppHost/`).
2. In the Aspire dashboard (or `aspire describe`), confirm every core resource reaches
   **Running**/Healthy: `api`, `bff`, `gari-kagada-client`, `garikaagada` (the PostgreSQL
   database), and `keycloak`; confirm `migrationworker` reaches **Finished** (not stuck
   Running). **Known limitation**: the SigNoz UI/otel-collector containers do not currently
   reach Healthy (a ClickHouse Keeper networking issue documented in AGENTS.md's "Scaffolding
   implementation notes") — this does not block or degrade the rest of the stack.
3. `curl -i http://<bff-address>/health` and `.../alive` on both `GariKaagada.BFF` and
   `GariKaagada.Api` — expect `200 OK` (see [contracts/health-and-ping-endpoints.md](./contracts/health-and-ping-endpoints.md)).
4. `curl -i http://<gari-kagada-client-address>/` — expect `200 OK` from the Angular dev
   server. This HTTP check, not just the dashboard reporting the process as "Running," is what
   "healthy" means for the frontend resource (clarification session 2026-07-03).
5. `curl -i http://<migrationworker-address>` in the dashboard — confirm it reached a
   **Finished** state (ran its, currently empty, migration and exited 0), not stuck "Running."
6. Stop with **`aspire stop`** (not Ctrl+C/`kill`, which only signals the CLI wrapper and can
   leave the frontend `npm run dev` process and the container-network tunnel proxy running —
   confirmed in testing). Confirm `aspire stop` fully terminates all non-persistent processes
   and containers. The backing-service containers declared with `WithLifetime(ContainerLifetime
   .Persistent)` (PostgreSQL, Keycloak, SigNoz's ClickHouse/keeper) are **expected to keep
   running** after `aspire stop` — this is intentional (it's what makes SC-006 possible below),
   not an orphan — proves SC-005 for every resource that isn't deliberately persistent.

Expected outcome: every resource healthy within 5 minutes of `aspire run` (SC-001); zero manual
steps beyond the prerequisites above (SC-003).

## Validate idempotent restart after a partial failure (SC-006)

1. Start `aspire run`; once the PostgreSQL/Keycloak/SigNoz containers are up but before the
   .NET services finish starting, force-kill the AppHost process (e.g., `kill -9`, not a clean
   Ctrl+C) to simulate a partial failure.
2. Run `aspire run` again, with no manual cleanup step in between (no `podman rm`, no volume
   deletion).
3. Confirm the run succeeds and every resource reaches healthy/running exactly as in the steps
   above — confirm via `podman ps` that the backing containers were **reused** (same container
   IDs/names as before the kill), not recreated from scratch, proving the persistent
   container lifetime + data volume wiring is doing its job (see
   [research.md](./research.md#decision-idempotent-restart-via-aspires-persistent-container-lifetime)).

Expected outcome: re-running after a partial failure always succeeds, with zero manual cleanup,
every time (SC-006).

## Validate User Story 2 — illegal layering fails at build time

1. Temporarily add a `ProjectReference` from `GariKaagada.Api.Data` to `GariKaagada.BFF` (an
   upward, illegal reference per Principle VII).
2. Run `dotnet build GariKaagada.slnx` — confirm the build fails (a circular/illegal reference
   error, not a silent success).
3. Revert the temporary reference; confirm `dotnet build GariKaagada.slnx` succeeds again.

Expected outcome: 100% of illegal cross-tier references are rejected at build time (SC-002).

## Validate User Story 3 — one shared type reaches the frontend automatically

1. With the stack running, `curl -X POST http://<bff-address>/api/ping -H "Content-Type:
   application/json" -d '{"message":"hello"}'` — expect `200 OK` with a `PingDto` body (see
   [contracts/health-and-ping-endpoints.md](./contracts/health-and-ping-endpoints.md) and
   [data-model.md](./data-model.md)).
2. `curl -X POST .../api/ping -d '{}'` (missing `message`) — expect `400 Bad Request`, proving
   `PingPayloadValidator` is actually enforced on the request path, not just defined.
3. In `gari-kagada-client/src/app/generated/`, confirm a generated TypeScript type/interface
   exists matching `PingPayload`/`PingDto`'s shape — confirm it was produced by the NSwag
   generation step (check the build log/output), not hand-written.
4. Delete the generated file and re-run the frontend build/generation step — confirm it
   regenerates identically, proving the pipeline is reproducible, not a one-off manual edit.

Expected outcome: a developer only ever authored the shape once, in `GariKaagada.Contracts`
(SC-004).
