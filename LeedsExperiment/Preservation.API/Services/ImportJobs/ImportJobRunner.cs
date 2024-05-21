using System.Text.Json;
using Fedora.Abstractions.Transfer;
using Preservation.API.Data;
using Preservation.API.Data.Entities;
using Preservation.API.Models;

namespace Preservation.API.Services.ImportJobs;

public class ImportJobRunner(
    PreservationContext dbContext,
    ModelConverter modelConverter,
    IPreservation storageService,
    ILogger<ImportJobRunner> logger)
{
    public async Task Execute(string importJobId, CancellationToken stoppingToken)
    {
        logger.LogInformation("This would execute {ImportJobId}", importJobId);
        var importJobEntity = await dbContext.ImportJobs.GetImportJob(importJobId, stoppingToken);

        if (importJobEntity == null)
        {
            logger.LogInformation("Import job {ImportJobId} not found", importJobId);
            return;
        }
        
        // Write DateBegun + save back to DB
        importJobEntity.DateBegun = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(stoppingToken);

        try
        {
            // Call Storage-API, which might take a while so need a longer timeout
            var isUpdate = IsUpdate(importJobEntity);
            var importJob = modelConverter.GetImportJob(importJobEntity);
            MakeParentsRelative(importJob);

            importJob.IsUpdate = await isUpdate;
            
            // TODO - should this be set in the StorageAPI?
            importJob.ArchivalGroupName = importJob.ArchivalGroupUri!.Slug();

            logger.LogInformation("Executing import job {ImportJobId}...", importJobId);
            var executedImportJob = await storageService.Import(importJob);
            
            // On return update the relevant fields in DB
            logger.LogInformation("Import job {ImportJobId} complete, updating DB..", importJobId);
            importJobEntity.BinariesDeleted = GetBinaryJson(executedImportJob.FilesDeleted);
            importJobEntity.BinariesAdded = GetBinaryJson(executedImportJob.FilesAdded);
            importJobEntity.BinariesPatched = GetBinaryJson(executedImportJob.FilesPatched);
            importJobEntity.ContainersAdded = GetContainerJson(executedImportJob.ContainersAdded);
            importJobEntity.ContainersDeleted = GetContainerJson(executedImportJob.ContainersDeleted);
            importJobEntity.NewVersion = executedImportJob.NewVersion?.OcflVersion;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error executing import job {ImportJobId}", importJobId);
            
            var error = new Error
            {
                Id = new Uri("https://sample.error/todo"),
                Message = ex.Message
            };

            importJobEntity.Errors = JsonSerializer.Serialize(error);
        }

        importJobEntity.DateFinished = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(stoppingToken);
    }

    private async Task<bool> IsUpdate(ImportJobEntity importJob)
    {
        try
        {
            var path = ArchivalGroupUriHelpers.GetArchivalGroupPath(importJob.DigitalObject);
            var resource = await storageService.GetArchivalGroup(path, null);
            return resource != null;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error checking if archival group for {ImportJobId} exists", importJob.Id);
            throw;
        }
    }

    private void MakeParentsRelative(ImportJob importJob)
    {
        // Preservation API doesn't care about anything other than the relative path in the preservation system
        var filesToDo = importJob.FilesToAdd.Union(importJob.FilesToDelete).Union(importJob.FilesToPatch);
        var containersToDo = importJob.ContainersToAdd.Union(importJob.ContainersToDelete);
        
        foreach (var r in filesToDo.Union<ResourceWithParentUri>(containersToDo))
        {
            r.Parent = ArchivalGroupUriHelpers.GetArchivalGroupRelativePath(r.Parent);
        }
    }

    private string GetBinaryJson(List<BinaryFile> binaryFiles)
    {
        var binaries = binaryFiles.Select(modelConverter.ToPresentationBinary).ToArray();
        return JsonSerializer.Serialize(binaries);
    }
    
    private string GetContainerJson(List<ContainerDirectory> containerDirectories)
    {
        var containers = containerDirectories.Select(modelConverter.ToPresentationContainer).ToArray();
        return JsonSerializer.Serialize(containers);
    }
}