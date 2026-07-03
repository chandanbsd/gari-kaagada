using System.Net.Http.Json;
using FluentValidation;
using GariKaagada.Contracts.Ping;
using Microsoft.AspNetCore.Mvc;

namespace GariKaagada.BFF.Controllers;

/// <summary>
/// Proves the BFF -> Api internal transport (constitution Principle VI) and the Contracts ->
/// NSwag -> generated-TypeScript pipeline (User Story 3) end-to-end. Not a product endpoint.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class PingController(IHttpClientFactory httpClientFactory, IValidator<PingPayload> validator) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<PingDto>> Post([FromBody] PingPayload payload, CancellationToken cancellationToken)
    {
        var validationResult = await validator.ValidateAsync(payload, cancellationToken);
        if (!validationResult.IsValid)
        {
            return ValidationProblem(new ValidationProblemDetails(validationResult.ToDictionary()));
        }

        var client = httpClientFactory.CreateClient("Api");
        var response = await client.PostAsJsonAsync("/api/ping", payload, cancellationToken);
        response.EnsureSuccessStatusCode();

        var dto = await response.Content.ReadFromJsonAsync<PingDto>(cancellationToken);
        return Ok(dto);
    }
}
