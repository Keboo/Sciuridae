using Microsoft.AspNetCore.Mvc;
using System.Text.RegularExpressions;

namespace Sciuridae.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class SquirrelController : ControllerBase
{
    private AppInformation AppInformation { get; }

    public SquirrelController(AppInformation appInformation)
    {
        AppInformation = appInformation ?? throw new ArgumentNullException(nameof(appInformation));
    }

    [HttpGet("{appName}/{channel}/{fileName}")]
    public async Task<IActionResult> Get(string appName, string channel, string fileName)
    {
        if (string.Equals("RELEASES", fileName, StringComparison.OrdinalIgnoreCase))
        {
            Uri? manifestUri = await AppInformation.GetFile(appName, channel, "RELEASES");
            if (manifestUri is not null)
            {
                return Redirect(manifestUri.AbsoluteUri);
            }
            else
            {
                return NotFound();
            }
        }
        var match = Regex.Match(fileName, @".+-(?<Version>\d+\.\d\.\d)-.+");
        if (!match.Success)
        {
            //TODO useful error
            return BadRequest();
        }
        string version = match.Groups["Version"].Value;
        Uri? fileUri = await AppInformation.GetFile(appName, channel, fileName, version);
        if (fileUri is null)
        {
            return NotFound();
        }
        return Redirect(fileUri.AbsoluteUri);
    }
}
