using GariKaagada.Api.Data;
using GariKaagada.MigrationWorker;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();

builder.AddNpgsqlDbContext<GariKaagadaDbContext>(connectionName: "garikaagada");

builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
