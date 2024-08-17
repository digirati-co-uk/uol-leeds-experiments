using Microsoft.AspNetCore.Mvc;
using Storage;

namespace Dashboard.Controllers
{
    public class OcflController : Controller
    {
        private readonly ILogger<OcflController> logger;
        private readonly IStorage storage;

        public OcflController(
            IStorage storage,
            ILogger<OcflController> logger)
        {
            this.storage = storage;
            this.logger = logger;
        }

        [ActionName("OcflIndex")]
        [Route("ocfl/{*path}")]
        public async Task<IActionResult> IndexAsync(
            [FromRoute] string path,
            [FromQuery] string? version = null)
        {
            ViewBag.Path = path;
            var ag = await storage.GetArchivalGroup(path, version);
            if (ag == null)
            {
                return NotFound();
            }
            if(ag.Type != "ArchivalGroup")
            {
                return BadRequest("Not an Archival Group");
            }

            return View("OcflArchivalGroup", ag);
        }
    }
}
