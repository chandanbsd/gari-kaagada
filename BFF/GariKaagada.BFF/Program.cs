using FluentValidation;
using GariKaagada.BFF.Hubs;
using GariKaagada.Contracts.Ping;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddSignalR();

// Validator classes live exclusively in GariKaagada.Contracts (Principle XI); DI registration
// happens here (frontend-facing) and again in GariKaagada.Api (defense-in-depth).
builder.Services.AddScoped<IValidator<PingPayload>, PingPayloadValidator>();

// Resolved via Aspire service discovery against the "api" resource name declared in
// GariKaagada.AppHost — proves the BFF -> Api internal transport is wired (Principle VI).
// Service discovery + resilience are applied automatically to every HttpClient by
// ServiceDefaults' AddServiceDefaults() above.
builder.Services.AddHttpClient("Api", client => client.BaseAddress = new Uri("https+http://api"));

var app = builder.Build();

app.MapDefaultEndpoints();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();
app.MapHub<AppHub>("/hubs/app");

app.Run();
