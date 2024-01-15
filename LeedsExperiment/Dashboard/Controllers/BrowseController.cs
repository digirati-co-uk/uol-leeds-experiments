using Fedora.Abstractions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Preservation;
using Utils;

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
        ViewBag.Path = path;
        var resource = await preservation.GetResource(path);
        if (resource == null)
        {
            return NotFound();
        }
        if(resource.ObjectPath != path)
        {
            if((resource.ObjectPath ?? string.Empty) == string.Empty && (path ?? string.Empty) == string.Empty)
            {
                // not a problem but rationalise this!
            }  
            else
            {
                return Problem("ObjectPath != path");
            }
        }
        if(resource.PreservationApiPartOf != null)
        {
            ViewBag.ArchivalGroupPath = preservation.GetInternalPath(resource.PreservationApiPartOf);
        }
        var reqPath = Request.Path.Value?.TrimEnd('/');
        if (reqPath.HasText())
        {
            ViewBag.Parent = reqPath[0..reqPath.LastIndexOf('/')];
        }
        switch (resource.Type)
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
