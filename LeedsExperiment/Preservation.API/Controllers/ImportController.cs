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


}
