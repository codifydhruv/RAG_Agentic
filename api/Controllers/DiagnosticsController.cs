using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/diagnostics")]
public class DiagnosticsController : ControllerBase
{
    [Authorize]
    [HttpGet("whoami")]
    public IActionResult WhoAmI()
    {
        var name = User.Identity?.Name ?? "unknown";
        var roles = User.Claims
            .Where(c => c.Type == "roles" || c.Type.EndsWith("/role"))
            .Select(c => c.Value);

        return Ok(new
        {
            Name = name,
            Roles = roles,
            AllClaims = User.Claims.Select(c => new { c.Type, c.Value })
        });
    }
}
