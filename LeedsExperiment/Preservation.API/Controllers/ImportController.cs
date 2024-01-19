using Amazon.S3;
using Fedora;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Preservation.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ImportController : Controller
{
    private readonly IStorageMapper storageMapper;
    private readonly IFedora fedora;
    private readonly PreservationApiOptions options;
    private IAmazonS3 s3Client;

    public ImportController(
        IStorageMapper storageMapper,
        IFedora fedora,
        IOptions<PreservationApiOptions> options,
        IAmazonS3 awsS3Client
    )
    {
        this.storageMapper = storageMapper;
        this.fedora = fedora;
        this.options = options.Value;
        this.s3Client = awsS3Client;
    }

    [HttpGet(Name = "ImportJob")]
    [Route("{*path}")]
    public async Task<ImportJob?> GetImportJob([FromRoute] string path, [FromQuery] string? source)
    {
        // build an import job, with checksums etc, set diffversion, time it.
        // compare the source with the object and build the diff properties.
        return null;
    }


    [HttpPost(Name = "ExecuteImport")]
    [Route("__import")]
    public async Task<ImportJob?> ExecuteImportJob([FromBody] ImportJob importJob)
    {
        // enter a transaction, check the version is the same, do all the stuff it says in the diffs, end transaction
        // keep a log of the updates (populate the *added props)
        // get the AG again, see the version, validate it's one on etc
        // return the import job
        return null;
    }
}
