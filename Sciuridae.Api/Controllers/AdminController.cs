using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Web.Resource;
using Sciuridae.Api.Data;
using System.ComponentModel.DataAnnotations;

namespace Sciuridae.Api.Controllers;

[ApiController]
[Authorize]
[Route("[controller]")]
[RequiredScope(RequiredScopesConfigurationKey = "AzureAd:Scopes")]
public class AdminController : ControllerBase
{
    private AppInformation AppInformation { get; }
    
    public AdminController(AppInformation appInformation)
    {
        AppInformation = appInformation ?? throw new ArgumentNullException(nameof(appInformation));
    }

    [HttpPost("register")]
    public async Task<IActionResult> RegisterApp([Required] string appName)
    {
        App? app = await AppInformation.Register(appName);
        if (app is not null)
        {
            return Ok(app.ApiKey);
        }
        return base.Problem("Failed to register app");
    }

    [HttpPost("regenerate-key")]
    public async Task<IActionResult> RegenerateKey([Required] string appName)
    {
        App? app = await AppInformation.CycleKey(appName);
        if (app is not null)
        {
            return Ok(app.ApiKey);
        }
        return base.NotFound("Failed to find app");
    }

    [HttpDelete("delete-release")]
    public async Task<IActionResult> DeleteRelease(
        [Required] string appName, 
        [Required] string version, 
        string? channel = null)
    {
        if (await AppInformation.DeleteRelease(appName, version, channel ?? Release.DefaultChannel))
        {
            return Ok($"Successfully removed {appName} {version}");
        }
        return BadRequest("Failed to remove release");
    }

}
