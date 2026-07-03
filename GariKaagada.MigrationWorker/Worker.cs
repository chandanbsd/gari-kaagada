using GariKaagada.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace GariKaagada.MigrationWorker;

/// <summary>
/// Runs pending EF Core migrations against <see cref="GariKaagadaDbContext"/> once at startup,
/// then stops the host so Aspire's <c>WaitForCompletion</c> can gate <c>GariKaagada.Api</c> on
/// this exiting 0 (constitution Principle XIII — the API project itself must never call
/// <c>Database.Migrate()</c> at runtime).
/// </summary>
public class Worker(
    IServiceProvider serviceProvider,
    IHostApplicationLifetime hostApplicationLifetime,
    ILogger<Worker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            using var scope = serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<GariKaagadaDbContext>();
            logger.LogInformation("Applying pending EF Core migrations for {DbContext}", nameof(GariKaagadaDbContext));
            await dbContext.Database.MigrateAsync(stoppingToken);
            logger.LogInformation("Migrations applied successfully for {DbContext}", nameof(GariKaagadaDbContext));
        }
        finally
        {
            hostApplicationLifetime.StopApplication();
        }
    }
}
