using GariKaagada.Contracts.Ping;
using Microsoft.AspNetCore.Mvc;

namespace GariKaagada.Api.Controllers;

/// <summary>
/// Proves the Contracts -> NSwag -> generated-TypeScript pipeline end-to-end (User Story 3).
/// Not a product/domain endpoint.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class PingController : ControllerBase
{
    [HttpPost]
    public ActionResult<PingDto> Post([FromBody] PingPayload payload)
    {
        return Ok(new PingDto(payload.Message, DateTime.UtcNow));
    }
}
