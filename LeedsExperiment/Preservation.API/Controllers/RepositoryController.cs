using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Preservation.API.Models;

namespace Preservation.API.Controllers;

[Route("[controller]/{*path}")]
[ApiController]
public class RepositoryController(
    IPreservation preservation,
    ModelConverter modelConverter,
    ILogger<RepositoryController> logger) : Controller
{
    /// <summary>
    /// Browse underlying repository for Container, DigitalObject or Binary.
    /// </summary>
    /// <param name="path">Path to item in repository to fetch (e.g. path/to/item</param>
    /// <param name="version">
    /// Optional version parameter, in format "v1","v2" etc. Current version returned if omitted
    /// </param>
    /// <returns><see cref="Container"/>, <see cref="DigitalObject"/> or <see cref="Binary"/></returns>
    [HttpGet(Name = "Browse")]
    [Produces("application/json")]
    public async Task<IActionResult> Browse([FromRoute] string path, [FromQuery] string? version = null)
    {
        var unEscapedPath = Uri.UnescapeDataString(path);
        var storageResource = string.IsNullOrEmpty(version)
            ? await preservation.GetResource(unEscapedPath)
            : await preservation.GetArchivalGroup(unEscapedPath, version);

        if (storageResource == null) return NotFound();

        var preservationResource =
            modelConverter.ToPreservationResource(storageResource, GetCorrectDisplayUrl());
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
        var storageContainer = await preservation.CreateContainer(unEscapedPath);
        
        var container = modelConverter.ToPreservationResource(storageContainer, GetCorrectDisplayUrl());
        return CreatedAtAction("Browse", new { path }, container);
    }
    
    public Uri GetCorrectDisplayUrl()
    {
        // HACK avoid `https://uol.digirati:80` when behind ELB 
        var displayUrl = HttpContext.Request.GetDisplayUrl();
        logger.LogInformation("DisplayUrl {UrlAsString}", displayUrl);
        var uri = new Uri(displayUrl);
        
        logger.LogInformation("Url Port and Scheme: {Port} and {Scheme}", uri.Scheme, uri.Port);
        if (uri is { Scheme: "https", Port: -1 or 80 })
        {
            var uriBuilder = new UriBuilder(uri) { Port = 443 };
            return uriBuilder.Uri;
        }

        return uri;
    }
}