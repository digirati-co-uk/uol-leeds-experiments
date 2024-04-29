using Fedora;
using Fedora.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace Storage.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ArchivalGroupController(IFedora fedora) : Controller
{
    /// <summary>
    /// Get details of specified Fedora archival group.
    /// An archival group represents a digital object e.g., the files
    /// that comprise a digitised book, or a manuscript, or a born digital item. An Archival Group might only have one
    /// file, or may contain hundreds of files and directories (e.g., digitised images and METS.xml)
    /// </summary>
    /// <param name="path">Path of Fedora archival group to fetch (e.g. path/to/item)</param>
    /// <param name="version">
    /// Archival group version to fetch (e.g. v1, v2 etc). Latest version returned if not specified
    /// </param>
    /// <returns>Details of archival group</returns>
    [HttpGet("{*path}", Name = "ArchivalGroup")]
    [Produces<ArchivalGroup>]
    [Produces("application/json")]
    public async Task<ActionResult<ArchivalGroup?>> Index(string path, string? version = null)
    {
        var ag = await fedora.GetPopulatedArchivalGroup(path, version);
        return ag;
    }
}