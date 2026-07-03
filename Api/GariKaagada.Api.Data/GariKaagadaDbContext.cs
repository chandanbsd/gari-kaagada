using Microsoft.EntityFrameworkCore;

namespace GariKaagada.Api.Data;

/// <summary>
/// The sole EF Core <see cref="DbContext"/> for GariKaagada, targeting the self-hosted
/// PostgreSQL instance (constitution Principle XIII). Intentionally has zero <see cref="DbSet{TEntity}"/>
/// properties — this is project-scaffolding work only; real entities are added by whichever
/// feature needs them first.
/// </summary>
public class GariKaagadaDbContext(DbContextOptions<GariKaagadaDbContext> options) : DbContext(options)
{
}
