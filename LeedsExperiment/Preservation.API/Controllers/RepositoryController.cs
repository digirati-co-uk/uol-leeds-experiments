using Fedora;
using Fedora.Abstractions;
using Fedora.Abstractions.Transfer;
using Microsoft.AspNetCore.Mvc;

namespace Preservation.API.Controllers
{
    [Route("api/repository/{*path}")]
    [ApiController]
    public class RepositoryController : Controller
    {
        private readonly IFedora fedora;

        public RepositoryController(IFedora fedora)
        {
            this.fedora = fedora;
        }

        [HttpGet]
        public async Task<ActionResult<Resource?>> Index(
            [FromRoute] string? path = null)
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

        [HttpPut]
        public async Task<ActionResult<Container?>> CreateContainer(
            [FromRoute] string path)
        {
            // DANGER - this needs to do the same kinds of checks at the Fedora level that the Dashboard is doing
            // This currently should only be used to create a container (not a binary) and only outside of an archival group.

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
}
