using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace Preservation.API.Controllers;

[Route("[controller]/{*path}")]
[ApiController]
public class RepositoryController(IPreservation preservation) : Controller
{
    /// <summary>
    /// Browse underlying repository for Container, DigitalObject or Binary.
    /// </summary>
    /// <returns></returns>
    [HttpGet]
    public async Task<IActionResult> Index([FromRoute] string path, [FromQuery] string? version = null)
    {
        // How do we know if this is an archive group or not?
        var storageResource = string.IsNullOrEmpty(version)
            ? await preservation.GetResource(path)
            : await preservation.GetArchivalGroup(path, version);

        if (storageResource == null) return NotFound();
        
        // Convert to appropriate type
        var preservationResource = storageResource.ToPreservationResource(new Uri(HttpContext.Request.GetDisplayUrl()));
        return Ok(preservationResource);
    }
}