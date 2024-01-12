using Microsoft.AspNetCore.Mvc;
using Preservation;

namespace Dashboard.Controllers
{
    public class OcflController : Controller
    {
        private readonly ILogger<OcflController> logger;
        private readonly IPreservation preservation;

        public OcflController(
            IPreservation preservation,
            ILogger<OcflController> logger)
        {
            this.preservation = preservation;
            this.logger = logger;
        }

        [ActionName("OcflIndex")]
        [Route("ocfl/{*path}")]
        public async Task<IActionResult> IndexAsync(
            [FromRoute] string path,
            [FromQuery] string? version = null)
        {
            ViewBag.Path = path;
            var ag = await preservation.GetArchivalGroup(path, version);
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
