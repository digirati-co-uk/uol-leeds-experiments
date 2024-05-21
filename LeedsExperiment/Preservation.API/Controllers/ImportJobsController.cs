using Fedora.Abstractions;
using Microsoft.AspNetCore.Mvc;
using Preservation.API.Data;
using Preservation.API.Data.Entities;
using Preservation.API.Models;
using Preservation.API.Services;
using Preservation.API.Services.ImportJobs;

namespace Preservation.API.Controllers;

[Route("deposits/{id}/[controller]")]
[ApiController]
public class ImportJobsController(
    PreservationContext dbContext,
    IPreservation preservation,
    IImportJobQueue importJobQueue,
    IImportService importService,
    IIdentityService identityService,
    ModelConverter modelConverter) : Controller
{
    /// <summary>
    /// Generate an <see cref="ImportJob"/> - a statement, in JSON form, of what changes you want carried out.
    /// Containers to add, Containers to delete, Binaries to add, Binaries to delete, Binaries to update.
    /// This will build an ImportJob for you, by comparing the DigitalObject (if it exists) with the content of the
    /// Deposit in S3 (S3 only - production implementation will use any known METS files for further metadata and
    /// structure).
    /// No changes will be made at this point. The ImportJob can be manually edited prior to submission if required.
    /// </summary>
    /// <param name="id">The identifier of a deposit to generate <see cref="ImportJob"/> for</param>
    /// <returns>JSON object representing changeset</returns>
    [HttpGet("diff")]
    public async Task<IActionResult> GenerateImportJob([FromRoute] string id, CancellationToken cancellationToken)
    {
        var start = DateTime.UtcNow;
            
        var existingDeposit = await dbContext.Deposits.GetDeposit(id, cancellationToken);
        var validationResult = ValidateDeposit(existingDeposit);
        if (validationResult != null) return validationResult;
        
        // TODO - handle versions
        
        // Get the as-is view from storage-api
        var existingArchivalGroup = await GetExistingArchivalGroup(existingDeposit);

        // is PreservationPath correct here? Does it need to be the fcrepo/ path, or derived from that at least?
        // I don't think so - the consumer of the PreservationAPI shouldn't ever know that path, we can rewrite on execute
        // but should that happen here or in StorageAPI? Doing it here for now
        var importJob = await importService.GetImportJob(existingArchivalGroup, existingDeposit.S3Root,
            existingDeposit.PreservationPath, start);
        
        var preservationImportJob = modelConverter.ToPreservationResource(importJob, id);
        return Ok(preservationImportJob);
    }

    /// <summary>
    /// Execute the instructions in the <see cref="ImportJob"/> to create/update/delete relevant resources from
    /// underlying storage
    /// </summary>
    /// <param name="id">The deposit Id this is related to</param>
    /// <param name="importJob">JSON instructions to be carried out</param>
    /// <returns>TODO - return val</returns>
    [HttpPost]
    public async Task<IActionResult> ExecuteImportJob([FromRoute] string id, [FromBody] PreservationImportJob importJob,
        CancellationToken cancellationToken)
    {
        // TODO - take a PreservationImportJob?
        var existingDeposit = await dbContext.Deposits.GetDeposit(id, cancellationToken);
        var validationResult = ValidateDeposit(existingDeposit);
        if (validationResult != null) return validationResult;

        importJob.DigitalObject =
            ArchivalGroupUriHelpers.GetArchivalGroupRelativePath(existingDeposit.PreservationPath)!;
        
        // Create a new identity
        var entity = new ImportJobEntity
        {
            Id = await identityService.MintImportJobIdentity(cancellationToken),
            OriginalImportJobId = new Uri("https://example.id/is-this-required"),
            Deposit = id,
            ImportJobJson = modelConverter.GetImportJson(importJob),
            DigitalObject = existingDeposit.PreservationPath,
        };
        dbContext.ImportJobs.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        
        // queue up for processing
        await importJobQueue.QueueRequest(entity, cancellationToken);
        
        // convert entity to ImportJobResult
        var importJobResult = modelConverter.ToImportJobResult(entity);

        return CreatedAtAction(nameof(GetImportJobResult), new { id, importJobId = entity.Id }, importJobResult);
    }

    [HttpGet("results/{importJobId}")]
    public async Task<IActionResult> GetImportJobResult([FromRoute] string id, [FromRoute] string importJobId,
        CancellationToken cancellationToken)
    {
        var entity = await dbContext.ImportJobs.GetImportJob(importJobId, cancellationToken);
        if (entity == null || !entity.Deposit.Equals(id, StringComparison.OrdinalIgnoreCase)) return NotFound();
        
        var importJobResult = modelConverter.ToImportJobResult(entity);
        return Ok(importJobResult);
    }

    private IActionResult? ValidateDeposit(DepositEntity? existingDeposit)
    {
        if (existingDeposit == null) return NotFound();
        if (existingDeposit.IsBeingExported()) return BadRequest("Deposit is being exported");
        if (existingDeposit.PreservationPath == null) return BadRequest("Deposit requires DigitalObject");
        return null;
    }

    private async Task<ArchivalGroup?> GetExistingArchivalGroup(DepositEntity existingDeposit)
    {
        var path = existingDeposit.PreservationPath.AbsolutePath;
        var storageResource = await preservation.GetArchivalGroup(path, null);
        return storageResource;
    }
}