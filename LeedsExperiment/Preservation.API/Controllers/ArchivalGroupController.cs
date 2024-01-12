using Fedora;
using Fedora.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace Preservation.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ArchivalGroupController : Controller
    {
        private IFedora fedora;

        public ArchivalGroupController(IFedora fedora)
        {
            this.fedora = fedora;
        }

        [HttpGet(Name = "ArchivalGroup")]
        [Route("{*path}")]
        public async Task<ActionResult<ArchivalGroup?>> Index(string path, string? version = null)
        {
            var ag = await fedora.GetPopulatedArchivalGroup(path, version);
            return ag;
        }
    }
}
