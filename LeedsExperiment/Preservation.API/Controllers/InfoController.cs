using Fedora;
using Fedora.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace Preservation.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class InfoController(IFedora fedora) : Controller
{
    /// <summary>
    /// Check if item at specified path exists in Fedora. If so get its type ("Container", "Binary" or "ArchivalGroup")
    /// </summary>
    /// <param name="path">Path to item in Fedora (e.g. path/to/item)</param>
    /// <returns><see cref="ResourceInfo"/> resource representing item at path</returns>
    [HttpGet("{*path}", Name = "GetInfo", Order = 1)]
    [Produces<ResourceInfo>]
    [Produces("application/json")]
    public async Task<ActionResult<ResourceInfo?>> Info(
        [FromRoute] string path)
    {
        var uri = fedora.GetUri(path);
        var info = await fedora.GetResourceInfo(uri);
        return info;
    }
}