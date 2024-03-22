using Fedora;
using Fedora.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace Preservation.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ArchivalGroupController(IFedora fedora) : Controller
    {
        private IFedora fedora = fedora;

        [HttpGet("{*path}", Name = "ArchivalGroup")]
        public async Task<ActionResult<ArchivalGroup?>> Index(string path, string? version = null)
        {
            var ag = await fedora.GetPopulatedArchivalGroup(path, version);
            return ag;
        }
    }
}
