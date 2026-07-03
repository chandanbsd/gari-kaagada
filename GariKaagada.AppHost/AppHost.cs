var builder = DistributedApplication.CreateBuilder(args);

// =============================================================================
// Backing services (constitution Principle II: self-hosted, Podman, Aspire-declared;
// FR-009/SC-006 clarification: major-version-pinned images + persistent lifetime + a data
// volume, so a killed/partial `aspire run` can always be re-run without manual cleanup).
// =============================================================================

var postgres = builder.AddPostgres("postgres")
    .WithImageTag("16")
    .WithLifetime(ContainerLifetime.Persistent)
    .WithDataVolume();

var gariKaagadaDb = postgres.AddDatabase("garikaagada");

// Keycloak's registry doesn't publish a bare major-version floating tag (no "26"), only
// major.minor (e.g. "26.6") — this is the closest equivalent to FR-009's major-version-pin
// intent for this image specifically; verified against Docker Hub at implementation time.
var keycloak = builder.AddKeycloak("keycloak")
    .WithImageTag("26.6")
    .WithLifetime(ContainerLifetime.Persistent)
    .WithDataVolume();

// =============================================================================
// SigNoz observability stack (constitution Principle XII: sole mandated OTLP backend).
//
// >>> KNOWN LIMITATION, VERIFIED IN PART <<<
// signoz/signoz-otel-collector's image bakes in a static /etc/otel/config.yaml with no
// env-var substitution (confirmed by extracting it from the image directly) — it hardcodes
// ClickHouse as reachable at "localhost"/"clickhouse", which is meaningless once ClickHouse is
// its own separate Aspire container. Fixed by bind-mounting a corrected copy
// (signoz-config/otel-collector-config.yaml, same file with only the two ClickHouse
// hostnames changed to "signoz-clickhouse", this container's Aspire resource name) — this WAS
// verified to fix the otel-collector's "connection refused" crash-loop against a running
// instance of this stack. The `signoz` (UI/query-service) container's own config schema is
// more involved (its own `--config` YAML, not simple env vars); confirmed end-to-end against
// a running instance (Postgres-backed sqlstore migrations applied, ClickHouse-backed
// telemetrystore reachable, query server listening on :8080) — see the DSN/env vars below.
//
// Neither SigNoz nor Keycloak publish bare-major floating tags (SigNoz: exact versions like
// "v0.131.1" only; Keycloak: major.minor like "26.6" only) — pinned to the current latest
// confirmed stable release as the closest available equivalent to FR-009's intent.
// =============================================================================

var signozPostgresPassword = builder.AddParameter("signoz-postgres-password", secret: true);
// ClickHouse's Docker image disables ALL network access for the "default" user unless
// CLICKHOUSE_PASSWORD (or CLICKHOUSE_USER) is set — confirmed directly from this container's
// own startup log ("neither CLICKHOUSE_USER nor CLICKHOUSE_PASSWORD is set, disabling network
// access for user 'default'"), which is what caused the otel-collector's "Authentication
// failed" error even with the hostname fixed. The same password is referenced in
// signoz-config/otel-collector-config.yaml's two datasource DSNs below.
// NOT builder.AddParameter(secret: true) with an auto-generated value: this password must
// also appear in the mounted signoz-config/otel-collector-config.yaml (a static file), which
// can't reference an Aspire-generated secret's runtime value. It's an internal credential
// between two containers on the same private Aspire network, never internet-facing.
var signozClickHousePassword = builder.AddParameter("signoz-clickhouse-password", value: "signoz-internal-dev-only");

var signozClickHouseKeeper = builder.AddContainer("signoz-clickhouse-keeper", "clickhouse/clickhouse-keeper", "25.5.6")
    .WithLifetime(ContainerLifetime.Persistent)
    .WithVolume("signoz-clickhouse-keeper-data", "/var/lib/clickhouse-keeper")
    // ClickHouse Keeper auto-merges override configs from a directory named after its main
    // config file's own basename ("keeper_config.d/"), not the generic "config.d/" that
    // clickhouse-server uses — confirmed by inspecting the running container: our old
    // config.d/listen.xml mount was silently ignored (never appeared in the merged/
    // preprocessed config), leaving Keeper's TCP listener on loopback-only and making it
    // unreachable from the separate signoz-clickhouse container, which broke the schema
    // migrator ("All connection tries failed while connecting to ZooKeeper").
    .WithBindMount("signoz-config/clickhouse-keeper-listen.xml", "/etc/clickhouse-keeper/keeper_config.d/listen.xml")
    .WithEndpoint(targetPort: 9181, name: "keeper-tcp");

var signozClickHouse = builder.AddContainer("signoz-clickhouse", "clickhouse/clickhouse-server", "25.5.6")
    .WithLifetime(ContainerLifetime.Persistent)
    .WithVolume("signoz-clickhouse-data", "/var/lib/clickhouse")
    .WithBindMount("signoz-config/clickhouse-keeper-config.xml", "/etc/clickhouse-server/config.d/zookeeper.xml")
    .WithEndpoint(targetPort: 9000, name: "ch-native")
    .WithEndpoint(targetPort: 8123, name: "ch-http")
    .WithEnvironment("CLICKHOUSE_PASSWORD", signozClickHousePassword)
    .WaitFor(signozClickHouseKeeper);

var signozPostgres = builder.AddContainer("signoz-postgres", "postgres", "16")
    .WithLifetime(ContainerLifetime.Persistent)
    .WithVolume("signoz-postgres-data", "/var/lib/postgresql/data")
    .WithEndpoint(targetPort: 5432, name: "pg")
    .WithEnvironment("POSTGRES_PASSWORD", signozPostgresPassword);

// One-shot schema migrator (official signoz/signoz-schema-migrator image, confirmed to exist
// and take a `sync --dsn` form) — creates the signoz_traces/signoz_metrics databases in
// ClickHouse that the otel-collector otherwise fails against with "Database ... does not
// exist". Gated with WaitForCompletion, same pattern as GariKaagada.MigrationWorker.
var signozSchemaMigrator = builder.AddContainer("signoz-schema-migrator", "signoz/signoz-schema-migrator", "v0.144.5")
    .WithArgs(
        "sync",
        "--dsn", "tcp://default:signoz-internal-dev-only@signoz-clickhouse:9000",
        // Our standalone (non-replicated) ClickHouse container's actual cluster name is
        // "default" (confirmed via `SELECT cluster FROM system.clusters`), not the migrator's
        // own default of "cluster" (meant for a real multi-node replicated deployment).
        "--cluster-name", "default")
    .WaitFor(signozClickHouse);

var signozOtelCollector = builder.AddContainer("signoz-otel-collector", "signoz/signoz-otel-collector", "v0.144.5")
    .WithBindMount("signoz-config/otel-collector-config.yaml", "/etc/otel/config.yaml")
    // scheme: "http" is required, not cosmetic — the same class of bug as the SigNoz UI link
    // above: without it, GetEndpoint("otlp-grpc") (used below for OTEL_EXPORTER_OTLP_ENDPOINT)
    // resolves to a "tcp://" URI. The .NET OTLP exporter accepts that URI without throwing,
    // but silently never delivers anything over it — confirmed by ClickHouse having zero rows
    // in signoz_traces/signoz_logs and zero non-"system.*" rows in signoz_metrics from any of
    // api/bff/migrationworker despite the pipeline otherwise reporting healthy. "http" (h2c,
    // no TLS) is correct here — this is a plaintext internal link between Aspire-managed
    // resources, matching OTEL_EXPORTER_OTLP_PROTOCOL=grpc's insecure default.
    .WithEndpoint(targetPort: 4317, name: "otlp-grpc", scheme: "http")
    // Fixed host port (not Aspire-allocated): the Angular dev-server's browser bundle can
    // only reach a published localhost port for its own OTLP/HTTP log export (see
    // gari-kagada-client's telemetry.ts) — it has no access to Aspire's container-network
    // DNS or to the dev-server process's environment variables.
    .WithEndpoint(port: 4318, targetPort: 4318, name: "otlp-http", scheme: "http")
    .WaitFor(signozClickHouse)
    .WaitForCompletion(signozSchemaMigrator);

var signoz = builder.AddContainer("signoz", "signoz/signoz", "v0.131.1")
    .WithLifetime(ContainerLifetime.Persistent)
    // scheme: "http" is required, not cosmetic — without it Aspire defaults the endpoint to
    // "tcp://", and the dashboard only renders http/https endpoint URLs as a clickable link
    // (a tcp:// URL shows as inert text), so the "SigNoz UI" link below silently had nothing
    // to click.
    .WithEndpoint(targetPort: 8080, name: "ui", scheme: "http")
    // SigNoz's real config schema (confirmed against its pkg/telemetrystore and pkg/sqlstore
    // Go source, not guessed): neither store has a *_HOST or *_PASSWORD key — the *_HOST/
    // *_PASSWORD env vars this container previously had were silently ignored, which is why
    // sqlstore fell back to its default (a local SQLite file, which then crashed with
    // "unable to open database file" since nothing here makes /var/lib/signoz writable/
    // persistent). Each store instead takes one full DSN with credentials embedded, and
    // Postgres requires SQLSTORE_PROVIDER set explicitly — its own default is "sqlite".
    .WithEnvironment("SIGNOZ_TELEMETRYSTORE_CLICKHOUSE_DSN",
        $"tcp://default:signoz-internal-dev-only@{signozClickHouse.GetEndpoint("ch-native").Property(EndpointProperty.HostAndPort)}")
    // Same "default" vs. the config's own "cluster" default that the schema migrator above
    // needed — this ClickHouse is a standalone single-node cluster actually named "default".
    .WithEnvironment("SIGNOZ_TELEMETRYSTORE_CLICKHOUSE_CLUSTER", "default")
    .WithEnvironment("SIGNOZ_SQLSTORE_PROVIDER", "postgres")
    .WithEnvironment("SIGNOZ_SQLSTORE_POSTGRES_DSN",
        $"postgres://postgres:{signozPostgresPassword}@{signozPostgres.GetEndpoint("pg").Property(EndpointProperty.HostAndPort)}/postgres?sslmode=disable")
    .WaitFor(signozClickHouse)
    .WaitFor(signozPostgres)
    .WithUrlForEndpoint("ui", url => url.DisplayText = "SigNoz UI");

// =============================================================================
// .NET services (constitution Principle VII: layered project architecture)
// =============================================================================

// Every runnable .NET service exports OTLP to *both* the Aspire dashboard (its own OTLP
// receiver, auto-injected by AddProject() as the default/unnamed OTEL_EXPORTER_OTLP_ENDPOINT —
// left untouched here) and SigNoz's collector (constitution Principle XII's mandated backend),
// via OTEL_EXPORTER_OTLP_SIGNOZ_ENDPOINT, a second, distinctly-named destination that
// ServiceDefaults' AddOpenTelemetryExporters() registers as a named "signoz" OTLP exporter
// alongside the default one. Deliberately NOT gated with .WaitFor(signozOtelCollector):
// telemetry export is a side effect, not a readiness dependency — the app must still start
// (and satisfy SC-001/User Story 1) even if the observability pipeline is degraded, per
// standard Aspire/observability practice of never blocking core app startup on a telemetry
// sink. The .NET OTLP exporter buffers/drops on its own if the collector isn't reachable yet.
var signozOtlpEndpoint = signozOtelCollector.GetEndpoint("otlp-grpc");

var migrationWorker = builder.AddProject<Projects.GariKaagada_MigrationWorker>("migrationworker")
    .WithReference(gariKaagadaDb)
    .WaitFor(gariKaagadaDb)
    .WithEnvironment("OTEL_EXPORTER_OTLP_SIGNOZ_ENDPOINT", signozOtlpEndpoint);

var api = builder.AddProject<Projects.GariKaagada_Api>("api")
    .WithReference(gariKaagadaDb)
    .WaitFor(gariKaagadaDb)
    .WaitForCompletion(migrationWorker)
    .WithEnvironment("OTEL_EXPORTER_OTLP_SIGNOZ_ENDPOINT", signozOtlpEndpoint)
    // Scalar's default route (Scalar.AspNetCore 2.x) for the "v1" document MapOpenApi()
    // already serves at /openapi/v1.json — dev-only, matching that call's own guard.
    .WithUrl("/scalar", "API Docs (Scalar)");

var bff = builder.AddProject<Projects.GariKaagada_BFF>("bff")
    .WithReference(api)
    .WaitFor(api)
    .WithReference(keycloak)
    .WaitFor(keycloak)
    .WithEnvironment("OTEL_EXPORTER_OTLP_SIGNOZ_ENDPOINT", signozOtlpEndpoint)
    .WithUrl("/scalar", "API Docs (Scalar)");

// =============================================================================
// Frontend (constitution Principle VII: gari-kagada-client is Aspire-orchestrated, not run
// by hand; clarification 2026-07-03: "healthy" means a real HTTP check on its dev server).
// =============================================================================

var gariKagadaClient = builder.AddJavaScriptApp("gari-kagada-client", "../gari-kagada-client")
    .WithReference(bff)
    .WaitFor(bff)
    .WithHttpEndpoint(env: "PORT")
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/");

#pragma warning disable ASPIREBROWSERLOGS001 // Experimental: opt-in for local Aspire-dashboard
// browser debugging (tracked Chromium session's console/network/screenshots). Unrelated to the
// SigNoz pipeline above — this never leaves the Aspire dashboard and doesn't capture real users.
gariKagadaClient.WithBrowserLogs();
#pragma warning restore ASPIREBROWSERLOGS001

builder.Build().Run();
