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
        var release = new Release(
            request.AppName,
            request.Tag ?? request.Version,
            request.Channel,
            request.Version,
            request.SetupFile ?? $"{request.AppName}Setup.exe")
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
        Release? release = await AppInformation.GetRelease(appName, channel ?? Release.DefaultChannel);
        
        if (release is not null)
        {
            return Ok(release.Version);
        }
        //Check if this is the first release of an app
        if (await AppInformation.GetApp(appName) is null)
        {
            return NotFound($"{appName} on channel {channel} not found");
        }
        //No releases but the app exists, allow empty string
        return Ok("");
    }

    [HttpGet("download/{appName}")]
    public async Task<IActionResult> Download(
        string appName, 
        [FromQuery] string? version = null,
        [FromQuery] string? channel = null)
    {
        channel ??= Release.DefaultChannel;
        Release? release = await AppInformation.GetRelease(appName, channel, version);
        if (release is null)
        {
            //Check if this is the first release of an app
            if (await AppInformation.GetApp(appName) is null)
            {
                return NotFound($"No release found for {appName} on channel {channel} @v{version}");
            }
            //The app exists but there are no releases
            return Ok(Array.Empty<string>());
        }

        Uri? manifestUri = await AppInformation.GetFile(release.AppName, release.Channel, "RELEASES");
        if (manifestUri is null)
        {
            return NotFound($"Could not find manifest URL for {release.AppName} on channel {channel}");
        }

        HttpClient client = new();
        string releases = await client.GetStringAsync(manifestUri);
        var latestReleases = ReleaseEntry.ParseReleaseFile(releases)
            .GroupBy(x => x.Version)
            .OrderByDescending(x => x.Key)
            .FirstOrDefault();
        if (latestReleases is null)
        {
            return NotFound("Could not determine latest release from the manifest");
        }

        List<string> files = new()
        {
            manifestUri.AbsoluteUri
        };
        foreach (var releaseEntry in latestReleases)
        {
            Uri? fileUri = await AppInformation.GetFile(appName, channel, releaseEntry.Filename, version);
            if (fileUri is not null)
            {
                files.Add(fileUri.AbsoluteUri);
            }
        }

        return Ok(files);
    }

    [HttpGet("download-setup/{appName}")]
    public async Task<IActionResult> DownloadSetup(
        string appName,
        [FromQuery] string? version = null,
        [FromQuery] string? channel = null)
    {
        channel ??= Release.DefaultChannel;
        Release? release = await AppInformation.GetRelease(appName, channel, version);
        if (release is null)
        {
            //Check if this is the first release of an app
            if (await AppInformation.GetApp(appName) is null)
            {
                return NotFound($"No release found for {appName} on channel {channel} @v{version}");
            }
            //The app exists but there are no releases
            return Ok(Array.Empty<string>());
        }

        Uri? setupFileUri = await AppInformation.GetFile(release.AppName, release.Channel, release.SetupFile);
        if (setupFileUri is null)
        {
            return NotFound($"Could not find setup file URL for {release.AppName} on channel {channel}");
        }
        return Redirect(setupFileUri.AbsoluteUri);
    }
}
