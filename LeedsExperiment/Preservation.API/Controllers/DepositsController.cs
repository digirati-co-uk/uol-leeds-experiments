using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Preservation.API.Data;
using Preservation.API.Data.Entities;
using Preservation.API.Models;

namespace Preservation.API.Controllers;

[Route("[controller]")]
[ApiController]
public class DepositsController(
    IAmazonS3 awsS3Client,
    PreservationContext dbContext,
    ModelConverter modelConverter,
    IOptions<PreservationSettings> preservationOptions,
    ILogger<DepositsController> logger)
    : Controller
{
    private readonly PreservationSettings preservationSettings = preservationOptions.Value;

    /// <summary>
    /// Create a new Deposit object. This will create a working location in s3 that will contain working set of files
    /// that will become a <see cref="DigitalObject"/>. 
    /// </summary>
    /// <param name="deposit">(Optional) Partial deposit object containing Preservation URI or submission text</param>
    /// <returns></returns>
    /// <remarks>
    /// Sample request:
    ///
    ///  Empty body, creates new Deposit
    ///     POST /deposits/
    ///     { }
    ///
    ///  Partial body, creates new Deposit specifying Preservation URI with optional submissionText
    ///     POST /deposits/
    ///     {
    ///       "digitalObject": "https://preservation.dlip.leeds.ac.uk/repository/example-objects/DigitalObject2",
    ///       "submissionText": "Just leaving this here"
    ///     }
    /// </remarks>
    [HttpPost]
    [Produces("application/json")]
    [Produces<Deposit>]
    public async Task<IActionResult> Create(Deposit? deposit = null, CancellationToken cancellationToken = default)
    {
        // NOTE: In reality it would be the Id service that generates this 
        var id = Identifiable.Generate();

        // create a key in S3
        var putObject = new PutObjectRequest
        {
            BucketName = preservationSettings.DepositBucket,
            Key = $"{preservationSettings}{id}/"
        };
        var putResult = await awsS3Client.PutObjectAsync(putObject, cancellationToken);
        if (putResult == null)
        {
            logger.LogError("Received empty response creating deposit at {BucketName}:{Key}", putObject.BucketName,
                putObject.Key);
            return new StatusCodeResult(500);
        }

        // SubmissionText + PreservationPath are optional
        var depositEntity = new DepositEntity
        {
            Id = id,
            Status = "new",
            S3Root = putObject.GetS3Uri(),
            SubmissionText = deposit?.SubmissionText,
            PreservationPath = deposit?.DigitalObject
        };
        dbContext.Deposits.Add(depositEntity);
        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogDebug("Created deposit {DepositId} in database", depositEntity.Id);

        var createdDeposit = modelConverter.ToDeposit(depositEntity);
        return Created(createdDeposit.Id, createdDeposit);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    [HttpGet("{id}", Name = "GetDeposit")]
    [Produces("application/json")]
    [Produces<Deposit>]
    public IActionResult Get([FromRoute] string id)
    {
        /*
         * empty body = create new Deposit + assign a new URI (from ID service)
         */
        throw new NotImplementedException();
    }
}

public static class S3Helpers
{
    public static Uri GetS3Uri(this PutObjectRequest putObjectRequest) =>
        new UriBuilder($"s3://{putObjectRequest.BucketName}") { Path = putObjectRequest.Key }.Uri;
}