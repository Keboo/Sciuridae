using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Web.Resource;
using Sciuridae.Api.Auth;
using Sciuridae.Api.Data;
using Squirrel;
using System.ComponentModel.DataAnnotations;

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

    [HttpPost("register")]
    [Authorize]
    [RequiredScope(RequiredScopesConfigurationKey = "AzureAd:Scopes")]
    public async Task<IActionResult> RegisterApp(
        [Required]string appName)
    {
        App? app = await AppInformation.Register(appName);
        if (app is not null)
        {
            return Ok(app.ApiKey);
        }
        return base.Problem("Failed to register app");
    }

    [HttpPost("release/github/v1")]
    [Authorize(HmacAuthenticationOptions.DefaultSchema, AuthenticationSchemes = HmacAuthenticationOptions.DefaultSchema)]
    public async Task<IActionResult> Update(UpdateGithubAppRequest request)
    {
        if (!string.Equals(User.Identity?.Name, request.AppName))
        {
            return BadRequest($"User '{User.Identity?.Name}' does not have permission to update {request.AppName}");
        }

        string repositoryUrl = request.RepositoryUrl;
        if (!repositoryUrl.EndsWith('/'))
        {
            repositoryUrl += '/';
        }
        var release = new Release(request.AppName, request.Tag ?? request.Version, request.Channel ?? "production", request.Version)
        {
            //TODO: Would be better to have the provider provide this
            Provider = "github",
            ProviderVersion = 1,
            ProviderData = $"{{ \"RepositoryUrl\": \"{repositoryUrl}\" }}"
        };
        if (await AppInformation.AddRelease(release))
        {
            return Ok();
        }
        return Problem("Failed to add release");
    }

    [HttpGet("version/{appName}")]
    public async Task<IActionResult> Version(string appName, [FromQuery]string? channel = null)
    {
        Release? release = await AppInformation.GetRelease(appName, channel ?? "production");
        
        if (release is not null)
        {
            return Ok(release.Version);
        }
        return NotFound();
    }

    [HttpGet("download/{appName}")]
    public async Task<IActionResult> Download(string appName, 
        [FromQuery] string? version = null,
        [FromQuery] string? channel = null)
    {
        channel ??= "production";
        Release? release = await AppInformation.GetRelease(appName, channel, version);
        if (release is null)
        {
            return NotFound($"No release found for {appName} on channel {channel} @v{version}");
        }

        Uri? manifestUri = await AppInformation.GetFile(release.AppName, release.Channel, "RELEASES");
        if (manifestUri is null)
        {
            return NotFound($"Could not find manifest URL for {release.AppName} on channel {channel}");
        }

        HttpClient client = new();
        string releases = await client.GetStringAsync(manifestUri);
        var latestRelease = ReleaseEntry.ParseReleaseFile(releases)
            .OrderByDescending(x => x.Version)
            .FirstOrDefault();
        if (latestRelease is null)
        {
            return NotFound("Could not find RELEASES file entry");
        }
        
        Uri? fileUri = await AppInformation.GetFile(appName, channel, latestRelease.Filename, version);
        if (fileUri is null)
        {
            return NotFound();
        }

        return Ok(new[]
        {
            fileUri.AbsoluteUri,
            manifestUri.AbsoluteUri
        });
    }
}
