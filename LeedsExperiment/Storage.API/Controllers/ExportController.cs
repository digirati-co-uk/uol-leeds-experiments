using Amazon.S3;
using Fedora;
using Fedora.Storage;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Preservation;

namespace Storage.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ExportController(
    IStorageMapper storageMapper,
    IFedora fedora,
    IOptions<StorageApiOptions> options,
    IAmazonS3 awsS3Client)
    : Controller
{
    private readonly StorageApiOptions options = options.Value;

    /// <summary>
    /// Export Fedora item to S3, optionally specifying version. Item is exported to configured staging bucket. 
    /// </summary>
    /// <param name="path">Path of Fedora item to export (e.g. path/to/item)</param>
    /// <param name="version">Optional version to export (e.g. v1, v2 etc). Latest version returned if not specified</param>
    /// <returns>JSON object representing result of export operation</returns>
    [HttpGet("{*path}", Name = "Export")]
    [Produces<ExportResult>]
    [Produces("application/json")]
    public async Task<ExportResult?> Index([FromRoute] string path, [FromQuery] string? version)
    {
        var exportKey = $"exports/{path}/{DateTime.Now:yyyy-MM-dd-HH-mm-ss}";
        return await ExportToLocation(path, version, exportKey);
    }

    /// <summary>
    /// Export Fedora item to S3, optionally specifying version. Item is exported to configured staging bucket and
    /// specific key. This is intended as an "internal only" call where the preservation-api can dictate which key the
    /// data is exported to. 
    /// </summary>
    /// <param name="path">Path of Fedora item to export (e.g. path/to/item)</param>
    /// <param name="destinationKey">
    /// Key in staging bucket where Fedora item to export be exported (e.g. path/to/item)
    /// </param>
    /// <param name="version">Optional version to export (e.g. v1, v2 etc). Latest version returned if not specified</param>
    /// <returns>JSON object representing result of export operation</returns>
    [HttpGet("internal/{*path}")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public async Task<ExportResult?> ControlledExport([FromRoute] string path, [FromQuery] string destinationKey,
        [FromQuery] string? version)
    {
        var exportKey = Uri.EscapeDataString(destinationKey);
        return await ExportToLocation(path, version, exportKey);
    }

    private async Task<ExportResult?> ExportToLocation(string path, string? version, string exportKey)
    {
        var agUri = fedora.GetUri(path);
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
                var resp = await awsS3Client.CopyObjectAsync(storageMap.Root, sourceKey, options.StagingBucket,
                    destKey);
                result.Files.Add($"s3://{options.StagingBucket}/{destKey}");
            }

            result.End = DateTime.Now;
        }
        catch (Exception ex)
        {
            result.Problem = ex.Message;
        }

        return result;
    }
}
