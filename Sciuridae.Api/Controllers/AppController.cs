using Microsoft.AspNetCore.Mvc;

namespace Sciuridae.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class AppController : ControllerBase
{
    [HttpGet("update/{appName}")]
    public IActionResult Update(string appName,
        string? tag = null,
        string? version = null,
        string? channel = null)
    {
        
        return Ok();
    }
}
