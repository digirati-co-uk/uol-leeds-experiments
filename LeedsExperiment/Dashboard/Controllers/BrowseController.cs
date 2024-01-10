using Fedora.Abstractions;
using Microsoft.AspNetCore.Mvc;
using Preservation;

namespace Dashboard.Controllers;

public class BrowseController : Controller
{
    private readonly ILogger<BrowseController> logger;
    private readonly IPreservation preservation;

    public BrowseController(
        IPreservation preservation,
        ILogger<BrowseController> logger)
    {
        this.preservation = preservation;
        this.logger = logger;
    }

    [Route("browse/{*path}")]
    public async Task<IActionResult> IndexAsync(string? path = null)
    {        
        var resource = await preservation.GetResource(path);
        if (resource == null)
        {
            return NotFound();
        }
        if(resource.PreservationApiPartOf != null)
        {
            ViewBag.ArchivalGroupPath = preservation.GetInternalPath(resource.PreservationApiPartOf);
        }
        switch(resource.Type)
        {
            case "Container":
            case "RepositoryRoot":
                return View("Container", resource as Container);
            case "Binary":
                return View("Binary", resource as Binary);
            case "ArchivalGroup":
                return View("ArchivalGroup", resource as ArchivalGroup);
            default:
                return Problem("Unknown Preservation type");
        }
    }
}
