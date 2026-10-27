
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;

namespace RoboReglament.Server.Controllers;

[ApiController]
[Route("api/security")]
public class SecurityController : ControllerBase
{
    private readonly IAntiforgery _antiforgery;

    public SecurityController(IAntiforgery antiforgery)
    {
        _antiforgery = antiforgery;
    }

    [HttpGet("csrf")]
    public IActionResult GetCsrfToken()
    {
        var tokens = _antiforgery.GetAndStoreTokens(
            HttpContext);

        Response.Headers["Cache-Control"] = "no-store";

        return Ok(new
        {
            token = tokens.RequestToken
        });
    }
}
