using Fedora;
using Fedora.Vocab;
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
            string? jsonld = Request.Query["jsonld"];
            // Unlike Fedora, we will default to COMPACTED
            string? jsonLdMode = JsonLdModes.Compacted;
            if (jsonld == "expanded") { jsonLdMode = JsonLdModes.Expanded; }
            if (jsonld == "flattened") { jsonLdMode = JsonLdModes.Flattened; }

            bool contained = Convert.ToBoolean(Request.Query["contained"]);

            // in WebAPI, path is not giving us the full path
            var fullPath = string.Join("/", Request.Path.ToString().Split('/')[5..]);
            var contentType = $"{contentTypeMajor}/{contentTypeMinor}";
            var result = await fedora.Proxy(contentType, fullPath, jsonLdMode, contained);
            return Content(result, contentType);
        }

        public 
    }
}
