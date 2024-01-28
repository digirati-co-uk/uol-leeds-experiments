using Fedora.Abstractions;
using Microsoft.AspNetCore.Mvc;
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

    [HttpGet]
    [Route("browse/{*path}")]
    public async Task<IActionResult> IndexAsync(string? path = null)
    {
        ViewBag.Path = path;
        var resource = await preservation.GetResource(path);
        if (resource == null)
        {
            return NotFound();
        }
        if (resource.ObjectPath != path)
        {
            if ((resource.ObjectPath ?? string.Empty) == string.Empty && (path ?? string.Empty) == string.Empty)
            {
                // not a problem but rationalise this!
            }
            else
            {
                return Problem("ObjectPath != path");
            }
        }
        if (resource.PreservationApiPartOf != null)
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

    [HttpPost]
    [Route("create/{*parentPath}")]
    public async Task<IActionResult> IndexAsync(
        [FromRoute] string? parentPath,
        [FromForm] string name)
    {
        if(parentPath == null)
        {
            parentPath = string.Empty; // repository root
        }
        ViewBag.Path = parentPath;
        var path = $"{parentPath.TrimEnd('/')}/{name}";
        var parentResource = await preservation.GetResource(parentPath);
        if(string.IsNullOrWhiteSpace(name))
        {
            ViewBag.Problem = $"No name supplied!";
            return View("Container", parentResource as Container);
        }
        if (!PathSafe.ValidPath(path))
        {
            ViewBag.Problem = $"{path} contains invalid characters";
            return View("Container", parentResource as Container);
        }
        if (parentResource!.Type == "ArchivalGroup" || parentResource.PreservationApiPartOf != null)
        {
            // We could allow this to happen if we want - need to experiment
            ViewBag.Problem = $"Can't create a container within an Archival Group - use an importJob please!";
            return View("Container", parentResource as Container);
        }
        var info = await preservation.GetResourceInfo(path);
        if (info.Exists)
        {
            ViewBag.Problem = $"Resource already exists at path {path}";
            return View("Container", parentResource as Container);
        }

        Container newContainer = await preservation.CreateContainer(path);
        ViewBag.Parent = "/browse/" + parentPath;


        ViewBag.CreateResult = $"Container created at path {path}";
        return View("Container", newContainer);
    }
}