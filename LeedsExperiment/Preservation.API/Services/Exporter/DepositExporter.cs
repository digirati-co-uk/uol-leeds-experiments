using System.Diagnostics;
using Storage;
using Preservation.API.Data;
using Preservation.API.Data.Entities;
using Utils;

namespace Preservation.API.Services.Exporter;

public class DepositExporter(IStorage storage, PreservationContext dbContext, ILogger<DepositExporter> logger)
{
    public async Task Export(ExportRequest exportRequest, CancellationToken cancellationToken)
    {
        var deposit = await GetDeposit(exportRequest.DepositId, cancellationToken);
        if (deposit == null) return;

        try
        {
            var exportKey = deposit.S3Root.AbsolutePath;
            var digitalObject = ArchivalGroupUriHelpers.GetArchivalGroupRelativePath(deposit.PreservationPath)!.OriginalString;
            var stopWatch = Stopwatch.StartNew();
            var exportResult = await storage.Export(digitalObject, exportRequest.Version, exportKey);
            stopWatch.Stop();

            logger.LogInformation("Export of deposit {Deposit} to {ExportKey} completed in {Elapsed}ms", deposit.Id,
                exportKey, stopWatch.ElapsedMilliseconds);
            logger.LogDebug("Export {Deposit} result: {@Export}", deposit.Id, exportResult);
            
            deposit.SetModified(nameof(DepositExporter));
            deposit.Status = DepositStates.Ready;
            deposit.DateExported = deposit.LastModified;
            deposit.VersionExported = exportResult.Version.OcflVersion;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred exporting {Deposit}", exportRequest);
            deposit.SetModified(nameof(DepositExporter));
            deposit.Status = DepositStates.ExportError;
            deposit.SubmissionText += $" Error exporting {ex.Message}";
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<DepositEntity?> GetDeposit(string depositId, CancellationToken cancellationToken)
    {
        logger.LogTrace("Processing queued export for {Deposit}", depositId);
        var deposit = await dbContext.Deposits.GetDeposit(depositId, cancellationToken);
        
        if (deposit == null)
        {
            logger.LogWarning("Unable to find Deposit with Id {Deposit}. Aborting export", depositId);
        }

        return deposit;
    }
}