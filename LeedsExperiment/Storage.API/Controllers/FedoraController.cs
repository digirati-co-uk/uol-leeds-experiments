using Fedora;
using Fedora.Vocab;
using Microsoft.AspNetCore.Mvc;

namespace Storage.API.Controllers;

[ApiController]
[Route("api/fedora")]
public class FedoraController(IFedora fedora) : Controller
{
    /// <summary>
    /// NOTE: This is for ease of browsing/familiarity only - NOT for application interactions.
    /// 
    /// Wrapper around Fedora API for ease of use. Accepts headers passed in URL to make conneg simpler in browser.
    /// 
    /// Supported contentTypes are: application/ld+json, application/n-triples, application/rdf+xml,
    /// application/x-turtle, text/html, text/n3, text/plain, text/rdf+n3, text/turtle
    /// </summary>
    /// <param name="contentTypeMajor">MediaType 'type' to use for conneg (e.g. for "text/n3" this is "text")</param>
    /// <param name="contentTypeMinor">MediaType 'subtype' to use for conneg (e.g. for "text/n3" this is "n3")</param>
    /// <param name="path">Path to item in Fedora (e.g. path/to/item)</param>
    /// <param name="jsonld">
    /// JsonLd mode to use, either "compacted", "expanded" or "flattened". Only applicable for application/ld+json.
    /// Defaults to "compacted" if not specified. 
    /// </param>
    /// <param name="contained">
    /// If 'true', response will be wrapped embed "child" resources in the returned representation. Optional.
    /// </param>
    /// <param name="acceptDate">Value to provide for "Accept-Datetime" header. Optional.</param>
    /// <param name="head">If 'true', resources headers only returned. Optional.</param>
    /// <returns>Fedora item represented by type specified by contentTypeMajor/contentTypeMinor</returns>
    [HttpGet("{contentTypeMajor}/{contentTypeMinor}/{*path}", Name = "FedoraProxy")]
    public async Task<IActionResult> Index(
        [FromRoute] string contentTypeMajor, 
        [FromRoute] string contentTypeMinor, 
        [FromRoute] string? path,
        [FromQuery] string? jsonld,
        [FromQuery] bool? contained,
        [FromQuery] string? acceptDate,
        [FromQuery] bool? head)
    {
        // Unlike Fedora, we will default to COMPACTED
        string jsonLdMode = JsonLdModes.Compacted;
        if (jsonld == "expanded") { jsonLdMode = JsonLdModes.Expanded; }
        if (jsonld == "flattened") { jsonLdMode = JsonLdModes.Flattened; }

        // in WebAPI, path is not giving us the full path
        var fullPath = Uri.UnescapeDataString(path ?? string.Empty);
        var contentType = $"{contentTypeMajor}/{contentTypeMinor}";

        var actualHead = head ?? false;
        
        var result = await fedora.Proxy(contentType, fullPath, jsonLdMode, contained == true, acceptDate, actualHead);
        if (actualHead == false)
        {
            return Content(result, "text/html");
        }
        return Content(result, contentType);
    }
        
}