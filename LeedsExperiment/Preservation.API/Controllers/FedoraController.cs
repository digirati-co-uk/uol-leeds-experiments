using Fedora;
using Microsoft.AspNetCore.Mvc;

namespace Preservation.API.Controllers
{
    [ApiController]
    [Route("api/fedora")]
    public class FedoraController : Controller
    {
        private IFedora fedora;

        public FedoraController(IFedora fedora) 
        {
            this.fedora = fedora;
        }

        [HttpGet(Name = "FedoraProxy")]
        [Route("{contentTypeMajor}/{contentTypeMinor}/{*path}")]
        public async Task<IActionResult> Index(string contentTypeMajor, string contentTypeMinor, string? path)
        {
            // in WebAPI, path is not giving us the full path
            var fullPath = string.Join("/", Request.Path.ToString().Split('/')[5..]);
            var contentType = $"{contentTypeMajor}/{contentTypeMinor}";
            var result = await fedora.Proxy(contentType, fullPath);
            return Content(result, contentType);
        }
    }
}
