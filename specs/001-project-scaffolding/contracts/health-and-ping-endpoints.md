# Contract: BFF HTTP endpoints (scaffolding-only)

These are the only HTTP surfaces this feature creates. All are proof-of-wiring endpoints, not
product features.

## `GET /health`, `GET /alive`

Provided automatically by `GariKaagada.ServiceDefaults`' `MapDefaultEndpoints()` on every
service (`GariKaagada.BFF`, `GariKaagada.Api`, `GariKaagada.MigrationWorker`). Not hand-written
by this feature — documented here because it's the mechanism User Story 1's acceptance scenario
("every declared project and backing service reaches a healthy/running state") relies on to be
observable from outside the process.

- **Request**: `GET /health` or `GET /alive`
- **Response**: `200 OK` when healthy; standard ASP.NET Core health-check JSON body.

## `POST /api/ping` (BFF only)

The one real endpoint this feature adds, existing solely to exercise the Contracts → NSwag →
TypeScript pipeline end-to-end (User Story 3 / FR-006 / SC-004). Calls straight through to a
matching endpoint on `GariKaagada.Api` via the internal HTTPS/gRPC transport (Principle VI),
proving the BFF → Api call path is wired — no business logic in either handler beyond
constructing the response.

- **Request**: `POST /api/ping`
  - Body: `PingPayload` (see [data-model.md](../data-model.md)) — `{ "message": string }`
  - Validated by `PingPayloadValidator` (`GariKaagada.Contracts`) before the handler runs.
- **Response**: `200 OK`
  - Body: `PingDto` — `{ "message": string, "receivedAtUtc": string (ISO-8601) }`
- **Errors**: `400 Bad Request` with the standard validation-failure shape if `message` is
  missing or exceeds 200 characters — proving FluentValidation is actually wired into the
  request pipeline, not just present as an unused class.

This endpoint's OpenAPI description is what NSwag generates `src/app/generated/` TypeScript
types from in `gari-kagada-client` (Principle XI) — there is no hand-written TypeScript
interface for `PingPayload`/`PingDto` anywhere in the frontend.

## SignalR hub: `AppHub` (BFF only)

Mapped at startup (Principle V/VI: "BFF... hosts the SignalR hub the frontend connects to") with
**zero hub methods**. Its only job in this feature is to prove the hub-hosting mechanism itself
is wired (the frontend's `@microsoft/signalr` client can open a connection to it); no real
events or methods exist until a feature needs to push a real notification.

- **Hub route**: `/hubs/app` (placeholder route; the real feature that needs SignalR may rename
  this — nothing depends on this exact path yet).
- **Methods**: none.
- **Client-invokable methods**: none.
