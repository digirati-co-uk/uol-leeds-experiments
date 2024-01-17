using Dashboard.Models;
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
            ViewBag.Path = path;
            var ag = await preservation.GetArchivalGroup(path, version);
            if (ag == null)
            {
                return NotFound();
            }
            if (ag.Type != "ArchivalGroup")
            {
                return BadRequest("Not an Archival Group");
            }
            return View("ExportStart", ag);
            // display a list of what's going to be exported
            // add a default destination in the staging bucket
            // allow a different destination to be specified (bucket, key root)

            // POST the job to /processexport (not the file list, that gets recalculated)

            // sync wait for response
            // in production this will go on a queue and will poll for completion

            // display summary completion, link back to ArchivalGroup Browse head
        }

        [HttpPost]
        [ActionName("ExportExecute")]
        [Route("export/{*path}")]
        public async Task<IActionResult> ExportExecuteAsync(
            [FromRoute] string path,
            [FromQuery] string? version = null)
        {
            var exportResult = await preservation.Export(path, version);
            return View("ExportResult", exportResult);
            // we could also supply the ag to the model for a richer display but at the expense of longer time
        }


        [HttpGet]
        [ActionName("ImportStart")]
        [Route("import/{*path}")]
        public async Task<IActionResult> ImportStartAsync(
            [FromRoute] string path,
            [FromQuery] string? source = null)
        {
            
            // remember the IsUpdate property on ImportJob
            // otherwise it's create new

            if (source != null)
            {
                var importJob = await preservation.GetUpdateJob(path, source);
                // render the importJob view with the import job ready to be executed
                return View("ImportJob", importJob);
            }
            // else render a view that asks for the S3 source then resubmits
            var existingAg = await preservation.GetArchivalGroup(path, null);
            // What's the best way to be really sure this DOES NOT EXIST?
            // It's allowed to be null here, for creating a new one.
            var model = new ImportStartModel { Path = path, ArchivalGroup = existingAg };
            return View("ImportStart", model);
        }
        

        [HttpGet]
        [ActionName("ImportExecute")]
        [Route("import/{*path}")]
        public async Task<IActionResult> ImportExecuteAsync(
            [FromBody] ImportJob importJob)
        {
            // what are we doing here - posting a big JSON job with files to update, delete etc.
            // That works nicely for new / creation - no deletes just additions

            // and it works nicely for custom non-diffs...
            // the dashboard can make this import job by generating a diff.
            // But I can make this import job outside the dashboard any way I like.

            var processedJob = await preservation.Import(importJob);
            return View("ImportResult", processedJob);
        }


    }




    // get a source bucket and root key from the user
    // show a list of the contents but don't diff yet

    // https://docs.aws.amazon.com/AmazonS3/latest/userguide/checking-object-integrity.html

    // S3 supports SHA256 but not 512
    // So we could lean on S3 to compute the checksums at source

    // can we set a bucket policy that auto-calculates a checkusm for every key when added?
    // otherwise we'll need to calculate the checksums ourselves

    // ask Leeds about 512 / 256

    // In order to calculate a diff we can compare checksums. We could compare sizes first to pick up obvious changes
    // but we'd need to checksum everything anyway

    // Generate a diff
    // files to add
    // files to change
    // files to rename? - can you even do that...
    // files to delete
    // This could be optimised in production
}
