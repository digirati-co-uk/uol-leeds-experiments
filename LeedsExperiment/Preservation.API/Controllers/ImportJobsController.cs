using Fedora.Abstractions;
using Microsoft.AspNetCore.Mvc;
using Preservation.API.Data;
using Preservation.API.Data.Entities;

namespace Preservation.API.Controllers;

[Route("deposits/{id}/[controller]")]
[ApiController]
public class ImportJobsController(
    PreservationContext dbContext,
    IPreservation preservation,
    IImportService importService) : Controller
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
        if (existingDeposit == null) return NotFound();
        if (existingDeposit.IsBeingExported()) return BadRequest("Deposit is being exported");
        if (existingDeposit.PreservationPath == null) return BadRequest("Must have DigitalObject to create diff");
        
        // TODO - handle versions
        
        // Get the as-is view from storage-api
        var existingArchivalGroup = await GetExistingArchivalGroup(existingDeposit);

        // is PreservationPath correct here? Does it need to be the fcrepo/ path, or derived from that at least?
        var importJob = await importService.GetImportJob(existingArchivalGroup, existingDeposit.S3Root,
            existingDeposit.PreservationPath, start);
        return Ok(importJob);
    }

    private async Task<ArchivalGroup?> GetExistingArchivalGroup(DepositEntity existingDeposit)
    {
        var path = existingDeposit.PreservationPath.AbsolutePath;
        var storageResource = await preservation.GetArchivalGroup(path, null);
        return storageResource;
    }
}