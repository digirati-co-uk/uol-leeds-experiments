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
    IExportQueue exportQueue,
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
    ///     POST /deposits/
    ///     { }
    ///
    ///     POST /deposits/
    ///     {
    ///       "digitalObject": "https://preservation.dlip.leeds.ac.uk/repository/example-objects/DigitalObject2",
    ///       "submissionText": "Just leaving this here"
    ///     }
    /// </remarks>
    [HttpPost]
    [Produces("application/json")]
    [Produces<Deposit>]
    public async Task<IActionResult> Create([FromBody] CreateDeposit? deposit = null,
        CancellationToken cancellationToken = default)
    {
        var depositEntity =
            await CreateNewDeposit(deposit?.SubmissionText, deposit?.DigitalObject, false, cancellationToken);
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
        var existingDeposit = await dbContext.Deposits.GetDeposit(id, cancellationToken);
        if (existingDeposit == null) return NotFound();
        if (existingDeposit.IsBeingExported()) return BadRequest("Deposit is being exported");

        if (changes.DigitalObject != null) existingDeposit.PreservationPath = changes.DigitalObject;
        if (changes.SubmissionText != null) existingDeposit.SubmissionText = changes.SubmissionText;
        if (changes.Status != null)
        {
            if (changes.Status.Equals(DepositStates.Exporting, StringComparison.OrdinalIgnoreCase) ||
                changes.Status.Equals(DepositStates.Ready, StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest($"{changes.Status} is a reserved status");
            }

            existingDeposit.Status = changes.Status;
        }

        existingDeposit.SetModified();

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
        var existingDeposit = await dbContext.Deposits.GetDeposit(id, cancellationToken);
        return existingDeposit == null ? NotFound() : Ok(existingDeposit);
    }

    /// <summary>
    /// Create a new deposit based on an existing object in Fedora repository. This will create a new Deposit, a working
    /// space in S3 and export all content from object specified in "digitalObject". The Deposit will return immediately
    /// but cannot be worked on until status is "ready". You may see object gradually populate into S3 key.
    /// "version" can optionally be specified, if omitted the latest version is exported.
    /// </summary>
    /// <param name="deposit">Details of DigitalObject to create a deposit from</param>
    /// <returns>Newly created <see cref="Deposit"/> object</returns>
    /// <remarks>
    /// Sample request:
    ///
    ///     POST /deposits/export
    ///     {
    ///       "digitalObject": "https://preservation.dlip.leeds.ac.uk/repository/example-objects/DigitalObject2"
    ///     }
    ///
    ///     POST /deposits/export
    ///     {
    ///       "digitalObject": "https://preservation.dlip.leeds.ac.uk/repository/example-objects/DigitalObject2",
    ///       "version": "v4"
    ///     }
    /// </remarks>
    [HttpPost("export")]
    [Produces("application/json")]
    [Produces<Deposit>]
    public async Task<IActionResult> Export([FromBody] ExportDeposit deposit, CancellationToken cancellationToken)
    {
        var depositEntity =
            await CreateNewDeposit("Created from export", deposit.DigitalObject, true, cancellationToken);

        // queue for export
        await exportQueue.QueueRequest(depositEntity, deposit, cancellationToken);

        var createdDeposit = modelConverter.ToDeposit(depositEntity);
        return Created(createdDeposit.Id, createdDeposit);
    }
    
    private async Task<DepositEntity> CreateNewDeposit(string? submissionText, Uri? digitalObject, bool isExport, 
        CancellationToken cancellationToken)
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
            throw new InvalidOperationException("AWS / S3 error");
        }

        // SubmissionText + PreservationPath are optional
        var depositEntity = new DepositEntity
        {
            Id = depositId,
            Status = isExport ? DepositStates.Exporting : DepositStates.New,
            S3Root = putObject.GetS3Uri(),
            SubmissionText = submissionText,
            PreservationPath = digitalObject, // TODO handle this/validation etc
            CreatedBy = "leedsadmin",
        };
        dbContext.Deposits.Add(depositEntity);
        await dbContext.SaveChangesAsync(cancellationToken);
        return depositEntity;
    }
}

// Using these models saves Swagger docs reading like all available fields are available for all actions
public class CreateDeposit
{
    public Uri? DigitalObject { get; set; }
    public string? SubmissionText { get; set; }
}

public class PatchDeposit : CreateDeposit
{
    public string? Status { get; set; }
}

public class ExportDeposit
{
    public Uri DigitalObject { get; set; }
    public string? Version { get; set; }
}