using System.Linq.Expressions;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Preservation.API.Data;
using Preservation.API.Data.Entities;
using Preservation.API.Models;
using Preservation.API.Services;

namespace Preservation.API.Controllers;

[Route("[controller]")]
[ApiController]
public class DepositsController(
    IAmazonS3 awsS3Client,
    PreservationContext dbContext,
    ModelConverter modelConverter,
    IIdentityService identityService,
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
    /// <returns>Newly created <see cref="Deposit"/> object</returns>
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
    public async Task<IActionResult> Create([FromBody] Deposit? deposit = null,
        CancellationToken cancellationToken = default)
    {
        var depositId = await identityService.MintNewIdentity(cancellationToken);

        // create a key in S3
        var putObject = new PutObjectRequest
        {
            BucketName = preservationSettings.DepositBucket,
            Key = $"{preservationSettings.DepositKeyPrefix}{depositId}/"
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
            Id = depositId,
            Status = "new",
            S3Root = putObject.GetS3Uri(),
            SubmissionText = deposit?.SubmissionText,
            PreservationPath = deposit?.DigitalObject, // TODO handle this/validation etc
            CreatedBy = "leedsadmin",
        };
        dbContext.Deposits.Add(depositEntity);
        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogDebug("Created deposit {DepositId} in database + S3 at '{DepositKey}'", depositEntity.Id,
            depositEntity.S3Root);

        var createdDeposit = modelConverter.ToDeposit(depositEntity);
        return Created(createdDeposit.Id, createdDeposit);
    }

    /// <summary>
    /// Update values of existing Deposit object. Used to set DigitalObject, SubmissionText and/or Status value.  
    /// </summary>
    /// <param name="id">Id of Deposit to PATCH</param>
    /// <param name="changes">Object containing values to update</param>
    /// <returns>Updated <see cref="Deposit"/> object</returns>
    /// <remarks>
    /// Sample request:
    ///
    ///     PATCH /deposits/xmpwer321
    ///     {
    ///       "digitalObject": "https://preservation.dlip.leeds.ac.uk/repository/example-objects/DigitalObject2",
    ///       "submissionText": "Just leaving this here",
    ///       "status": "processing"
    ///     }
    /// </remarks>
    [HttpPatch("{id}")]
    [Produces("application/json")]
    [Produces<Deposit>]
    public async Task<IActionResult> Create([FromRoute] string id, [FromBody] PatchDeposit changes,
        CancellationToken cancellationToken)
    {
        var existingDeposit = await dbContext.Deposits.FindAsync([id], cancellationToken);
        if (existingDeposit == null) return NotFound();

        if (changes.DigitalObject != null) existingDeposit.PreservationPath = changes.DigitalObject;
        if (changes.SubmissionText != null) existingDeposit.SubmissionText = changes.SubmissionText;
        if (changes.Status != null) existingDeposit.Status = changes.Status;
        existingDeposit.LastModified = DateTime.UtcNow;
        existingDeposit.LastModifiedBy = "leedsadmin";

        await dbContext.SaveChangesAsync(cancellationToken);
        return Ok(existingDeposit);
    }

    /// <summary>
    /// Get existing deposit with specified Id
    /// </summary>
    /// <param name="id">Id of Deposit to fetch</param>
    /// <returns><see cref="Deposit"/> object</returns>
    [HttpGet("{id}", Name = "GetDeposit")]
    [Produces("application/json")]
    [Produces<Deposit>]
    public async Task<IActionResult> Get([FromRoute] string id, CancellationToken cancellationToken)
    {
        var existingDeposit = await dbContext.Deposits.FindAsync([id], cancellationToken);
        return existingDeposit == null ? NotFound() : Ok(existingDeposit);
    }
}

public class PatchDeposit
{
    public Uri? DigitalObject { get; set; }
    public string? SubmissionText { get; set; }
    public string? Status { get; set; }
}
