using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Storage.API.Models;

namespace Storage.API.Controllers;

[Route("[controller]/{*path}")]
[ApiController]
public class RepositoryController(IStorage storage, ModelConverter modelConverter) : Controller
{
    /// <summary>
    /// Browse underlying repository for Container, DigitalObject or Binary.
    /// </summary>
    /// <param name="path">Path to item in repository to fetch (e.g. path/to/item</param>
    /// <param name="version">
    /// Optional version parameter, in format "v1","v2" etc. Current version returned if omitted. NOTE: not fully
    /// implemented in demo, will always return latest version.
    /// </param>
    /// <returns>Container, DigitalObject or Binary</returns>
    [HttpGet(Name = "Browse")]
    [ProducesResponseType<Container>(200, "application/json")]
    [ProducesResponseType<Binary>(200, "application/json")]
    [ProducesResponseType<DigitalObject>(200, "application/json")]
    public async Task<IActionResult> Browse([FromRoute] string path, [FromQuery] string? version = null)
    {
        var unEscapedPath = Uri.UnescapeDataString(path);
        var storageResource = string.IsNullOrEmpty(version)
            ? await storage.GetResource(unEscapedPath)
            : await storage.GetArchivalGroup(unEscapedPath, version);

        if (storageResource == null) return NotFound();

        var preservationResource =
            modelConverter.ToPreservationResource(storageResource, new Uri(HttpContext.Request.GetDisplayUrl()));
        return Ok(preservationResource);
    }

    /// <summary>
    /// Create Container in underlying repository, at specified path. This is _not_ a directory within a DigitalObject
    /// but rather to hierarchically organise content in the repository (note: this currently is not enforced).
    /// In production this would be restricted to a small subset of users/applications.
    /// </summary>
    /// <param name="path">Path of Container to create</param>
    /// <returns>Newly create container</returns>
    [HttpPost]
    [Produces("application/json")]
    [Produces<Container>]
    public async Task<IActionResult> CreateContainer([FromRoute] string path)
    {
        var unEscapedPath = Uri.UnescapeDataString(path);
        var storageContainer = await storage.CreateContainer(unEscapedPath);
        
        var container = modelConverter.ToPreservationResource(storageContainer, new Uri(HttpContext.Request.GetDisplayUrl()));
        return CreatedAtAction("Browse", new { path }, container);
    }
}