using Microsoft.AspNetCore.Mvc;
using Preservation;

namespace Dashboard.Controllers
{
    public class ImportExportController : Controller
    {
        private readonly ILogger<ImportExportController> logger;
        private readonly IPreservation preservation;

        public ImportExportController(
            IPreservation preservation,
            ILogger<ImportExportController> logger)
        {
            this.preservation = preservation;
            this.logger = logger;
        }

        [HttpGet]
        [ActionName("ExportStart")]
        [Route("export/{*path}")]
        public async Task<IActionResult> ExportStartAsync(
            [FromRoute] string path,
            [FromQuery] string? version = null)
        {
        }


        [HttpGet]
        [ActionName("ImportStart")]
        [Route("import/{*path}")]
        public async Task<IActionResult> ImportStartAsync(
            [FromRoute] string path,
            [FromQuery] string? version = null)
        {
        }
    }
}
