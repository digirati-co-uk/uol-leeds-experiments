namespace Preservation.API.Services;

/// <summary>
/// Listens to queue export and handles export process
/// </summary>
public class DepositExporter(IExportQueue exportQueue, ILogger<DepositExporter> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation($"Starting {nameof(DepositExporter)}");

        await BackgroundProcessor(stoppingToken);
    }
    
    private async Task BackgroundProcessor(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var depositEntity = await exportQueue.DequeueRequest(stoppingToken);

            try
            {
                logger.LogTrace("Processing queued export for {Deposit}", depositEntity.Id);
                // TODO do the export. Mark as 'ready' when done
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred exporting {Deposit}", depositEntity.Id);
                // TODO mark as errored in DB. Drop an error into S3 if failed?
            }
        }
    }
}