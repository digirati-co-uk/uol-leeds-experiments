using Fedora;
using Fedora.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace Preservation.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RepositoryController : Controller
    {
        private IFedora fedora;

        public RepositoryController(IFedora fedora)
        {
            this.fedora = fedora;
        }

        [HttpGet(Name = "Browse")]
        [Route("{*path}")]
        public async Task<ActionResult<Resource?>> Index(string? path = null)
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
    }
}
