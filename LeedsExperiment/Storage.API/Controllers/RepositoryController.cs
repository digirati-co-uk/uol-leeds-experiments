using Fedora;
using Fedora.Abstractions;
using Fedora.Abstractions.Transfer;
using Microsoft.AspNetCore.Mvc;
using Storage;

namespace Storage.API.Controllers;

[Route("api/repository/{*path}")]
[ApiController]
public class RepositoryController(IFedora fedora) : Controller
{
    /// <summary>
    /// Get JSON representation of item at path.
    /// </summary>
    /// <param name="path">Path to item in Fedora (e.g. path/to/item). Optional</param>
    /// <returns><see cref="Resource"/> representing item at path</returns>
    [HttpGet]
    [Produces<Resource>]
    [Produces("application/json")]
    public async Task<ActionResult<Resource?>> Index([FromRoute] string? path = null)
    {
        Resource? resource;
        if (string.IsNullOrEmpty(path))
        {
            resource = await fedora.GetRepositoryRoot();
        }
        else
        {
            if (path.EndsWith("/"))
            {
                var fullPathString = Request.Path.ToString().TrimEnd('/');
                return Redirect(fullPathString);
            }
            resource = await fedora.GetObject(path);
        }
        return resource;
    }

    /// <summary>
    /// Create a new "Container" in Fedora at specified path
    ///  
    /// DANGER - this does not have same kinds of checks at the Fedora level that the Dashboard is doing
    /// This currently should only be used to create a container (not a binary) and only outside of an archival group.
    /// </summary>
    /// <param name="path">Path of new Container to create in Fedora (e.g. path/to/item)</param>
    /// <returns>Newly created <see cref="Container"/></returns>
    [HttpPut]
    [Produces<Container>]
    [Produces("application/json")]
    public async Task<ActionResult<Container?>> CreateContainer([FromRoute] string path)
    {
        var npp = new NameAndParentPath(path);
        var cd = new ContainerDirectory() { 
            Name = npp.Name, 
            Parent = fedora.GetUri(npp.ParentPath ?? string.Empty), 
            Path = npp.Name 
        };

        var newContainer = await fedora.CreateContainer(cd);
        return newContainer;
    }

}