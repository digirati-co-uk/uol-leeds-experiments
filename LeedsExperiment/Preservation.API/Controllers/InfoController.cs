using Fedora;
using Fedora.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace Preservation.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class InfoController : Controller
    {
        private readonly IFedora fedora;

        public InfoController(IFedora fedora)
        {
            this.fedora = fedora;
        }


        [HttpGet("{*path}", Name = "GetInfo", Order = 1)]
        public async Task<ActionResult<ResourceInfo?>> Info(
        [FromRoute] string path)
        {
            var uri = fedora.GetUri(path);
            var info = await fedora.GetResourceInfo(uri);
            return info;
        }
    }

}
