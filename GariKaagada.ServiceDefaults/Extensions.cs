using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ServiceDiscovery;
using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace Microsoft.Extensions.Hosting;

// Adds common Aspire services: service discovery, resilience, health checks, and OpenTelemetry.
// This project should be referenced by each service project in your solution.
// To learn more about using this project, see https://aka.ms/aspire/service-defaults
public static class Extensions
{
    private const string HealthEndpointPath = "/health";
    private const string AlivenessEndpointPath = "/alive";

    public static TBuilder AddServiceDefaults<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        builder.ConfigureOpenTelemetry();

        builder.AddDefaultHealthChecks();

        builder.Services.AddServiceDiscovery();

        builder.Services.ConfigureHttpClientDefaults(http =>
        {
            // Turn on resilience by default
            http.AddStandardResilienceHandler();

            // Turn on service discovery by default
            http.AddServiceDiscovery();
        });

        // Uncomment the following to restrict the allowed schemes for service discovery.
        // builder.Services.Configure<ServiceDiscoveryOptions>(options =>
        // {
        //     options.AllowedSchemes = ["https"];
        // });

        return builder;
    }

    public static TBuilder ConfigureOpenTelemetry<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        builder.Logging.AddOpenTelemetry(logging =>
        {
            logging.IncludeFormattedMessage = true;
            logging.IncludeScopes = true;
        });

        builder.Services.AddOpenTelemetry()
            .WithMetrics(metrics =>
            {
                metrics.AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation();
            })
            .WithTracing(tracing =>
            {
                tracing.AddSource(builder.Environment.ApplicationName)
                    .AddAspNetCoreInstrumentation(tracing =>
                        // Exclude health check requests from tracing
                        tracing.Filter = context =>
                            !context.Request.Path.StartsWithSegments(HealthEndpointPath)
                            && !context.Request.Path.StartsWithSegments(AlivenessEndpointPath)
                    )
                    // Uncomment the following line to enable gRPC instrumentation (requires the OpenTelemetry.Instrumentation.GrpcNetClient package)
                    //.AddGrpcClientInstrumentation()
                    .AddHttpClientInstrumentation();
            });

        builder.AddOpenTelemetryExporters();

        return builder;
    }

    private static TBuilder AddOpenTelemetryExporters<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        // NOTE: We deliberately never call the OpenTelemetryBuilder.UseOtlpExporter()
        // convenience method used by the default Aspire template. It registers a guard that
        // throws NotSupportedException ("Signal-specific AddOtlpExporter methods and the
        // cross-cutting UseOtlpExporter method being invoked on the same IServiceCollection is
        // not supported") the moment any signal-specific AddOtlpExporter(...) call also exists
        // in the same container — confirmed by GariKaagada.MigrationWorker crashing on startup
        // (unhandled exception, exit code 134) the first time this was mixed with the second
        // ("signoz") destination below. Since two OTLP destinations are required, both are
        // wired through the same signal-specific AddOtlpExporter(name, configure) API instead —
        // the unnamed overload reads the standard OTEL_EXPORTER_OTLP_* env vars exactly like
        // UseOtlpExporter() would, just without tripping its cross-cutting guard.
        var useOtlpExporter = !string.IsNullOrWhiteSpace(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]);

        if (useOtlpExporter)
        {
            // Default/unnamed OTLP destination: Aspire's AddProject() auto-injects
            // OTEL_EXPORTER_OTLP_ENDPOINT (+ headers) for every project resource, pointing at
            // the Aspire dashboard's own OTLP receiver — this is what populates the
            // dashboard's own Structured Logs/Traces/Metrics pages.
            builder.Services.AddOpenTelemetry()
                .WithTracing(tracing => tracing.AddOtlpExporter())
                .WithMetrics(metrics => metrics.AddOtlpExporter());
            builder.Logging.AddOpenTelemetry(logging => logging.AddOtlpExporter());
        }

        // Second, named OTLP destination: SigNoz (constitution Principle XII's mandated
        // backend), read from a distinctly-named env var (see GariKaagada.AppHost/AppHost.cs)
        // rather than overwriting OTEL_EXPORTER_OTLP_ENDPOINT above — so telemetry reaches
        // both the Aspire dashboard and SigNoz, not one or the other.
        var signozEndpoint = builder.Configuration["OTEL_EXPORTER_OTLP_SIGNOZ_ENDPOINT"];
        if (!string.IsNullOrWhiteSpace(signozEndpoint))
        {
            var signozUri = new Uri(signozEndpoint);
            void ConfigureSignozOtlp(OtlpExporterOptions o)
            {
                o.Endpoint = signozUri;
                o.Protocol = OtlpExportProtocol.Grpc;
            }

            builder.Services.AddOpenTelemetry()
                .WithTracing(tracing => tracing.AddOtlpExporter("signoz", ConfigureSignozOtlp))
                .WithMetrics(metrics => metrics.AddOtlpExporter("signoz", ConfigureSignozOtlp));

            builder.Logging.AddOpenTelemetry(logging => logging.AddOtlpExporter("signoz", ConfigureSignozOtlp));
        }

        // Uncomment the following lines to enable the Azure Monitor exporter (requires the Azure.Monitor.OpenTelemetry.AspNetCore package)
        //if (!string.IsNullOrEmpty(builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"]))
        //{
        //    builder.Services.AddOpenTelemetry()
        //       .UseAzureMonitor();
        //}

        return builder;
    }

    public static TBuilder AddDefaultHealthChecks<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        builder.Services.AddHealthChecks()
            // Add a default liveness check to ensure app is responsive
            .AddCheck("self", () => HealthCheckResult.Healthy(), ["live"]);

        return builder;
    }

    public static WebApplication MapDefaultEndpoints(this WebApplication app)
    {
        // Adding health checks endpoints to applications in non-development environments has security implications.
        // See https://aka.ms/aspire/healthchecks for details before enabling these endpoints in non-development environments.
        if (app.Environment.IsDevelopment())
        {
            // All health checks must pass for app to be considered ready to accept traffic after starting
            app.MapHealthChecks(HealthEndpointPath);

            // Only health checks tagged with the "live" tag must pass for app to be considered alive
            app.MapHealthChecks(AlivenessEndpointPath, new HealthCheckOptions
            {
                Predicate = r => r.Tags.Contains("live")
            });
        }

        return app;
    }
}
