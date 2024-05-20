namespace Preservation.API.Services.ImportJobs;

public class ImportJobExecutorService(
    IServiceScopeFactory serviceScopeFactory,
    IImportJobQueue importJobQueue,
    ILogger<ImportJobExecutorService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation($"Starting {nameof(ImportJobExecutorService)}");

        while (!stoppingToken.IsCancellationRequested)
        {
            var importJobId = await importJobQueue.DequeueRequest(stoppingToken);
            
            using var scope = serviceScopeFactory.CreateScope();
            var processor = scope.ServiceProvider.GetRequiredService<ImportJobRunner>();
            await processor.Execute(importJobId, stoppingToken);
        }
    }
}