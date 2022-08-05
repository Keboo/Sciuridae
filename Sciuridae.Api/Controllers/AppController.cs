using Microsoft.AspNetCore.Mvc;

namespace Sciuridae.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class AppController : ControllerBase
{
    private AppInformation AppInformation { get; }
    
    public AppController(AppInformation appInformation)
    {
        AppInformation = appInformation ?? throw new ArgumentNullException(nameof(appInformation));
    }

    [HttpGet("update/{appName}")]
    public async Task<IActionResult> Update(
        string appName,
        string version,
        string? tag = null,
        string? channel = null)
    {
        await AppInformation.AddRelease(appName, channel ?? "production", version);

        return Ok();
    }
}
