using Microsoft.AspNetCore.Mvc;

namespace Storage.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SourceController : Controller
    {
        private readonly IImportService s3ImportService;

        public SourceController(IImportService s3ImportService)
        {
            this.s3ImportService = s3ImportService;
        }

        /// <summary>
        /// Examine files hosted at 'source' and return information about them.
        /// </summary>
        /// <param name="source">S3 URI containing items to create diff from (e.g. s3://uol-expts-staging-01/ocfl-example)</param>
        /// <returns>ImportSource JSON payload</returns>
        [HttpGet(Name = "ImportSource")]
        [Produces<ImportSource>]
        [Produces("application/json")]
        public async Task<ImportSource?> GetImportSource([FromQuery] string source)
        {
            var importSource = await s3ImportService.GetImportSource(new Uri(source));
            return importSource;
        }


        /// <summary>
        /// Copy files hosted at 'source' to a new location, adding a checksum in the process.
        /// Return information about them.
        /// We have to copy because you can't apply a checksum to an existing S3 key.
        /// </summary>
        /// <param name="source">S3 URI containing items to copy (e.g. s3://uol-expts-staging-01/ocfl-example)</param>
        /// <returns>ImportSource JSON payload for new location</returns>
        [HttpPost("copy", Name = "CopyImportSource")]
        [Produces<ImportSource>]
        [Produces("application/json")]
        public async Task<ImportSource?> CopyToNewSourceWithChecksums([FromQuery] string source)
        {
            var importSource = await s3ImportService.CopyToNewSourceWithChecksums(new Uri(source));
            return importSource;
        }
    }
}
