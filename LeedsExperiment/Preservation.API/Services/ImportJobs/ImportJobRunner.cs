using Preservation.API.Data;

namespace Preservation.API.Services.ImportJobs;

public class ImportJobRunner(
    PreservationContext dbContext,
    ILogger<ImportJobRunner> logger)
{
    public async Task Execute(string importJobId, CancellationToken stoppingToken)
    {
        logger.LogInformation("This would execute {ImportJobId}", importJobId);
        var importJob = await dbContext.ImportJobs.GetImportJob(importJobId, stoppingToken);
        
        // Set pickup date + write back to DB
        
        // Call Storage-API, which might take a while so need a longer timeout
        
        // On return update the relevant fields in DB
    }
}