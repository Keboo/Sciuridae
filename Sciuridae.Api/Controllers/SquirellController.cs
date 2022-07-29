using Microsoft.AspNetCore.Mvc;
using System.Text.RegularExpressions;

namespace Sciuridae.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class SquirellController : ControllerBase
{
    [HttpGet("{appName}/{channel}/{fileName}{query?}")]
    public IActionResult Get(string appName, string channel, string fileName)
    {
        //https://localhost:7155/Squirell/SimplyBudget/production/RELEASES
        //https://localhost:7155/Squirrel/SimplyBudget/production/RELEASES?arch=x64&os=windows&id=SimplyBudget&localVersion=1.0.1
        string? rootRepo = AppMap.GetRepo(appName);
        if (rootRepo is null)
        {
            return NotFound();
        }
        Uri fileUri = new Uri(rootRepo, UriKind.Absolute);
        if (string.Equals("RELEASES", fileName, StringComparison.OrdinalIgnoreCase))
        {
            //https://github.com/Keboo/SimplyBudget/releases/latest/download/RELEASES
            fileUri = new Uri(fileUri, "releases/latest/download/RELEASES");
            return Redirect(fileUri.AbsoluteUri);
        }
        var match = Regex.Match(fileName, @".+-(?<Version>\d+\.\d\.\d)-.+");
        if (!match.Success)
        {
            //TODO useful error
            return BadRequest();
        }
        //https://github.com/Keboo/SimplyBudget/releases/download/0.0.0/SimplyBudget-1.0.0-full.nupkg
        string version = match.Groups["Version"].Value;
        fileUri = new Uri(fileUri, $"releases/download/{version}/{fileName}");
        return Redirect(fileUri.AbsoluteUri);
    }
}
