using Fedora;
using Fedora.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace Preservation.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ArchivalGroupController(IFedora fedora) : Controller
{
    /// <summary>
    /// Get details of specified Fedora archival group
    /// </summary>
    /// <param name="path">Path of Fedora archival group to fetch</param>
    /// <param name="version">Archival group version to fetch. Latest version returned if not specified</param>
    /// <returns>Details of archival group</returns>
    [HttpGet("{*path}", Name = "ArchivalGroup")]
    [Produces<ArchivalGroup>]
    public async Task<ActionResult<ArchivalGroup?>> Index(string path, string? version = null)
    {
        var ag = await fedora.GetPopulatedArchivalGroup(path, version);
        return ag;
    }
}