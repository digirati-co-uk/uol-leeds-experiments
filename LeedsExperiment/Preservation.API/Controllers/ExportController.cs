using Amazon.S3;
using Fedora;
using Fedora.Storage;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Preservation.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ExportController : Controller
{
    private readonly IStorageMapper storageMapper;
    private readonly IFedora fedora;
    private readonly PreservationApiOptions options;
    private IAmazonS3 s3Client;

    public ExportController(
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

    [HttpGet(Name = "Export")]
    [Route("{*path}")]
    public async Task<ExportResult?> Index([FromRoute] string path, [FromQuery] string? version)
    {
        var agUri = fedora.GetUri(path);
        var exportKey = $"exports/{path}/{DateTime.Now:yyyy-MM-dd-HH-mm-ss}";
        var storageMap = await storageMapper.GetStorageMap(agUri, version);
        var result = new ExportResult
        {
            ArchivalGroupPath = path,
            Destination = $"s3://{options.StagingBucket}/{exportKey}",
            StorageType = StorageTypes.S3,
            Version = storageMap.Version,
            Start = DateTime.Now
        };
        try
        {
            foreach (var file in storageMap.Files)
            {
                var sourceKey = $"{storageMap.ObjectPath}/{file.Value.FullPath}";
                var destKey = $"{exportKey}/{file.Key}";
                var resp = await s3Client.CopyObjectAsync(storageMap.Root, sourceKey, options.StagingBucket, destKey);
                result.Files.Add($"s3://{options.StagingBucket}/{destKey}");
            }
            result.End = DateTime.Now;
        }
        catch(Exception ex)
        {
            result.Problem = ex.Message;
        }
        return result;
    }
}
