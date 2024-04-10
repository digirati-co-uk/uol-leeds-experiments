using Microsoft.AspNetCore.Mvc;

namespace Preservation.API.Controllers;

[Route("")]
[ApiController]
public class ValuesController : ControllerBase
{
    /// <summary>
    /// Ignore - health-check endpoint
    /// </summary>
    [HttpGet]
    [Produces<string>]
    [Produces("application/json")]
    public string Get() => "I'm alive";
}