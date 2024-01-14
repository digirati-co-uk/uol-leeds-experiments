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
            // display a list of what's going to be exported
            // add a default destination in the staging bucket
            // allow a different destination to be specified (bucket, key root)

            // POST the job to /processexport (not the file list, that gets recalculated)

            // sync wait for response
            // in production this will go on a queue and will poll for completion

            // display summary completion, link back to ArchivalGroup Browse head
        }


        [HttpGet]
        [ActionName("ImportStart")]
        [Route("import/{*path}")]
        public async Task<IActionResult> ImportStartAsync(
            [FromRoute] string path,
            [FromQuery] string? version = null)
        {
            // work out what can be shared with a create new
            // (is it just this with a null path?)

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


    }
}
