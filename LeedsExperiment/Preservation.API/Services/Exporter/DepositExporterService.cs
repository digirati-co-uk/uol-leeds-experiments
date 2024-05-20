namespace Preservation.API.Services.Exporter;

/// <summary>
/// Listens to queue export and handles export process
/// </summary>
public class DepositExporterService(
    IServiceScopeFactory serviceScopeFactory,
    IExportQueue exportQueue,
    ILogger<DepositExporterService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation($"Starting {nameof(DepositExporterService)}");

        while (!stoppingToken.IsCancellationRequested)
        {
            var exportRequest = await exportQueue.DequeueRequest(stoppingToken);
            
            using var scope = serviceScopeFactory.CreateScope();
            var processor = scope.ServiceProvider.GetRequiredService<DepositExporter>();
            await processor.Export(exportRequest, stoppingToken);
        }
    }
}