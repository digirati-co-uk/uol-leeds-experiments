using Dashboard.Models;
using Fedora.Abstractions;
using Microsoft.AspNetCore.Mvc;
using Storage;
using System.Text.Json;

namespace Dashboard.Controllers
{
    public class ImportExportController : Controller
    {
        private readonly ILogger<ImportExportController> logger;
        private readonly IStorage storage;

        public ImportExportController(
            IStorage storage,
            ILogger<ImportExportController> logger)
        {
            this.storage = storage;
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
            var ag = await storage.GetArchivalGroup(path, version);
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
            var exportResult = await storage.Export(path, version);
            return View("ExportResult", exportResult);
            // we could also supply the ag to the model for a richer display but at the expense of longer time
        }


        [HttpGet]
        [ActionName("ImportStart")]
        [Route("import/{*path}")]
        public async Task<IActionResult> ImportStartAsync(
            [FromRoute] string path,
            [FromQuery] string? source = null,
            [FromQuery] string? name = null,
            [FromQuery] string? validateSource = null) // name if creating a new one; can't rename atm
        {
            var resourceInfo = await storage.GetResourceInfo(path);
            var model = new ImportModel { Path = path, ResourceInfo = resourceInfo };
            if (resourceInfo.Type == nameof(ArchivalGroup))
            {
                // This is an update to an existing archival group
                var existingAg = await storage.GetArchivalGroup(path, null);
                model.ArchivalGroup = existingAg;
            }
            else if (resourceInfo.StatusCode != 404)
            {
                ViewBag.Problem = "Invalid Status code: " + resourceInfo.StatusCode;
                return View("ImportStart", model);
            }
            if (validateSource == "on")
            {
                model.ImportSource = await storage.GetImportSource(source);
                if(model.ImportSource != null && model.ImportSource.Files.Any(x => string.IsNullOrWhiteSpace(x.Digest)))
                {
                    ViewBag.Problem = "At least one source file lacks a Digest";
                }
                return View("ImportStart", model);
            }
            model.Name = name;
            if (!string.IsNullOrWhiteSpace(source))
            {
                // we already know where the update file are
                // This is only for synchronising with S3; There'll need to be a different route
                // for ad hoc construction of an ImportJob (e.g., it's just one deletion).
                var importJob = await storage.GetUpdateJob(path, source);
                model.ImportJob = importJob;
                importJob.ArchivalGroupName = model.ArchivalGroup?.Name ?? model.Name;
                return View("ImportJob", model);
            }
            // else render a view that asks for the S3 source then resubmits
            return View("ImportStart", model);

            // Create a new AG from anywhere by GET to here at path and name;
            // form that adds path and submits a GET;
            // form prevents conflicting path;
            // doesn't appear under AG
        }



        [HttpPost]
        [ActionName("CopySource")]
        [Route("copysource/{*path}")]
        public async Task<IActionResult> CopyToNewSourceWithChecksums(
            [FromRoute] string path,
            [FromForm] string? source = null,
            [FromForm] string? name = null,
            [FromForm] string? copySource = null)
        {
            var newImportSource = await storage.CopyToNewSourceWithChecksums(source);
            return await ImportStartAsync(path, newImportSource?.Source.ToString(), name, "on");
        }



        [HttpPost]
        [ActionName("ImportExecute")]
        [Route("import/{*path}")]
        public async Task<IActionResult> ImportExecuteAsync(
           //  [FromBody] ImportJob importJob
           [FromForm] string importJobString
        )
        {
            // This could accept JSON data directly but we want to fiddle about with it.
            var importJob = JsonSerializer.Deserialize<ImportJob>(importJobString);
            if(importJob == null)
            {
                return Problem("Could not find an import job in request");
            }

            // what are we doing here - posting a big JSON job with files to update, delete etc.
            // That works nicely for new / creation - no deletes just additions

            // and it works nicely for custom non-diffs...
            // the dashboard can make this import job by generating a diff.
            // But I can make this import job outside the dashboard any way I like.
            // Would probably post directly to the Preservation API

            var processedJob = await storage.Import(importJob);
            var resultingAg = await storage.GetArchivalGroup(importJob.ArchivalGroupPath, null);
            var model = new ImportModel
            {
                ImportJob = processedJob,
                ArchivalGroup = resultingAg,
                Path = importJob.ArchivalGroupPath
            };
            return View("ImportResult", model);
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
