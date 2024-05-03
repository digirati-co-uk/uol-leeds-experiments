using System.Diagnostics;
using Preservation.API.Data;
using Preservation.API.Data.Entities;

namespace Preservation.API.Services;

/// <summary>
/// Listens to queue export and handles export process
/// </summary>
public class DepositExporter(
    IExportQueue exportQueue,
    IPreservation preservation,
    PreservationContext dbContext,
    ILogger<DepositExporter> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation($"Starting {nameof(DepositExporter)}");

        while (!stoppingToken.IsCancellationRequested)
        {
            var exportRequest = await exportQueue.DequeueRequest(stoppingToken);
            await DoExport(exportRequest, stoppingToken);
        }
    }

    private async Task DoExport(ExportRequest exportRequest, CancellationToken cancellationToken)
    {
        var deposit = await GetDeposit(exportRequest.DepositId, cancellationToken);
        if (deposit == null) return;

        try
        {
            var exportKey = deposit.S3Root.AbsolutePath;
            var digitalObject = deposit.PreservationPath!.AbsolutePath;
            var stopWatch = Stopwatch.StartNew();
            var exportResult = await preservation.Export(digitalObject, exportRequest.Version, exportKey);
            stopWatch.Stop();

            logger.LogInformation("Export of deposit {Deposit} to {ExportKey} completed in {Elapsed}ms", deposit.Id,
                exportKey, stopWatch.ElapsedMilliseconds);
            
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
            deposit.SubmissionText += $"Error exporting {ex.Message}";
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