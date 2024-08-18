using Fedora;
using Fedora.Abstractions;
using Fedora.ApiModel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Storage.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ImportController : Controller
{
    private readonly IFedora fedora;
    private readonly IImportService s3ImportService;
    private readonly ILogger<ImportController> logger;
    private readonly FedoraApiOptions fedoraApiSettings;

    public ImportController(
        IFedora fedora,
        IImportService s3ImportService,
        IOptions<FedoraApiOptions> fedoraApiOptions,
        ILogger<ImportController> logger
    )
    {
        this.fedora = fedora;
        this.s3ImportService = s3ImportService;
        fedoraApiSettings = fedoraApiOptions.Value;
        this.logger = logger;
    }

    /// <summary>
    /// Build an 'importJob' payload for an existing archival group by comparing it to files hosted at 'source'.
    /// </summary>
    /// <param name="archivalGroupPath">Path to item in Fedora (e.g. path/to/item)</param>
    /// <param name="source">S3 URI containing items to create diff from (e.g. s3://uol-expts-staging-01/ocfl-example)</param>
    /// <returns>Import job JSON payload</returns>
    [HttpGet("{*archivalGroupPath}", Name = "ImportJob")]
    [Produces<ImportJob>]
    [Produces("application/json")]
    public async Task<ImportJob?> GetImportJob([FromRoute] string archivalGroupPath, [FromQuery] string source)
    {
        var agUri = fedora.GetUri(archivalGroupPath);
        var diffStart = DateTime.UtcNow;

        var archivalGroup = await GetValidatedArchivalGroupForImportJob(agUri);

        // This is either an existing Archival Group, or a 404 where the immediate parent is a Container that is not itself part of an Archival Group.
        // So now evaluate the source:
        var sourceUri = new Uri(Uri.UnescapeDataString(source));
        var importJob = await s3ImportService.GetImportJob(archivalGroup, sourceUri, agUri, diffStart);
        return importJob;
    }

    /// <summary>
    /// Returns an archivalGroup if there is one at archivalGroupUri
    /// Returns null if there isn't one there
    /// Throws an exception if it's not possible to _create_ an archival group there.
    /// </summary>
    /// <param name="archivalGroupUri"></param>
    /// <param name="transaction"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    private async Task<ArchivalGroup?> GetValidatedArchivalGroupForImportJob(Uri archivalGroupUri, Transaction? transaction = null)
    {
        ArchivalGroup? archivalGroup = null;
        var info = await fedora.GetResourceInfo(archivalGroupUri, transaction);
        if (info.Exists && info.Type == nameof(ArchivalGroup))
        {
            archivalGroup = await fedora.GetPopulatedArchivalGroup(archivalGroupUri, null, transaction);
        }
        else if (info.StatusCode == 404) // HTTP leakage
        {
            archivalGroup = null;
            // it doesn't exist - but we still need to check that:
            // - it has an immediate parent container
            // - that container is not itself an archival group or part of one
            var npp = new NameAndParentPath(archivalGroupUri.IsAbsoluteUri
                ? archivalGroupUri.AbsolutePath
                : archivalGroupUri.OriginalString);
            if (npp.ParentPath == null)
            {
                throw new InvalidOperationException($"No parent object for {archivalGroupUri}");
            }
            var parentInfo = await fedora.GetObject(npp.ParentPath, transaction);
            if (parentInfo == null)
            {
                throw new InvalidOperationException($"No parent object for {archivalGroupUri}");
            }
            if (parentInfo.Type == nameof(ArchivalGroup))
            {
                throw new InvalidOperationException($"The parent of {archivalGroupUri} is an Archival Group");
            }
            if (parentInfo.PartOf != null)
            {
                throw new InvalidOperationException($"{archivalGroupUri} is already part of an Archival Group");
            }
            if (parentInfo.Type != nameof(Container))
            {
                throw new InvalidOperationException($"The parent of {archivalGroupUri} is not a container");
            }
        }
        else
        {
            throw new InvalidOperationException($"Cannot create {archivalGroupUri} for {info.Type}, status: {info.StatusCode}");
        }

        return archivalGroup;
    }

    /// <summary>
    /// Make changes to Fedora as outlined in importJob payload.
    ///
    /// See GET /api/import for endpoint to help generate payload. 
    /// </summary>
    /// <param name="importJob">JSON payload containing details of changes to make</param>
    /// <returns>Processed <see cref="ImportJob"/></returns>
    /// <remarks>
    /// Sample request:
    ///
    ///     POST: /api/__import
    ///     {
    ///         "archivalGroupPath": "Example",
    ///         "source": "s3://uol-expts-staging-01/ocfl-example",
    ///         "storageType": "S3",
    ///         "archivalGroupUri": "https://uol.digirati.io/fcrepo/rest/Example",
    ///         "archivalGroupName": null,
    ///         "diffStart": "2024-04-10T16:49:58.038852+01:00",
    ///         "diffEnd": "2024-04-10T16:50:10.1859735+01:00",
    ///         "diffVersion": null,
    ///         "containersToAdd": [{
    ///             "path": "foo",
    ///             "parent": "https://uol.digirati.io/fcrepo/rest/Example"
    ///             "name": "foo",
    ///             "slug": "foo"
    ///         }],
    ///         "filesToAdd": [],
    ///         "filesToDelete": [],
    ///         "filesToPatch": [],
    ///         "containersToDelete": [],
    ///         "containersAdded": [],
    ///         "filesAdded": [],
    ///         "filesDeleted": [],
    ///         "filesPatched": [],
    ///         "containersDeleted": [],
    ///         "isUpdate": false
    ///     }
    /// </remarks>
    [HttpPost("__import", Name = "ExecuteImport")]
    [Produces<ImportJob>]
    [Produces("application/json")]
    public async Task<ImportJob?> ExecuteImportJob([FromBody] ImportJob importJob)
    {
        logger.LogInformation("Executing import job {path}", importJob.ArchivalGroupPath);

        // enter a transaction, check the version is the same, do all the stuff it says in the diffs, end transaction
        // keep a log of the updates (populate the *added props)
        // get the AG again, see the version, validate it's one on etc
        // return the import job
        if (importJob.ArchivalGroupUri == null)
        {
            throw new InvalidOperationException("No URI supplied for ArchivalGroup");
        }

        // Consumer has only supplied to AG path - prepend the Fedora path
        importJob.ArchivalGroupUri = EnsureFedoraPath(importJob.ArchivalGroupUri);
        
        importJob.Start = DateTime.Now;
        var transaction = await fedora.BeginTransaction();
        ArchivalGroup? archivalGroup = await GetValidatedArchivalGroupForImportJob(importJob.ArchivalGroupUri, transaction);

        if (!importJob.IsUpdate)
        {
            if(archivalGroup != null)
            {
                await fedora.RollbackTransaction(transaction);
                throw new InvalidOperationException("An Archival Group has recently been created at this URI");
            }

            if (string.IsNullOrWhiteSpace(importJob.ArchivalGroupName))
            {
                await fedora.RollbackTransaction(transaction);
                throw new InvalidOperationException("No name supplied for this archival group");
            }

            archivalGroup = await fedora.CreateArchivalGroup(
                importJob.ArchivalGroupUri.Parent(), 
                importJob.ArchivalGroupUri.Slug(), 
                importJob.ArchivalGroupName, transaction);

            if (archivalGroup == null)
            {
                await fedora.RollbackTransaction(transaction);
                throw new InvalidOperationException("No archival group was returned from creation");
            }
        }

        // We need to keep the transaction alive throughout this process
        // will need to time operations and call fedora.KeepTransactionAlive

        try
        {
            foreach (var container in importJob.ContainersToAdd.OrderBy(cd => cd.Path))
            {
                container.Parent = EnsureFedoraPath(container.Parent);
                logger.LogInformation("Creating container {path}", container.Path);
                var fedoraContainer = await fedora.CreateContainer(container, transaction);
                logger.LogInformation("Container created at {location}", fedoraContainer!.Location);
                importJob.ContainersAdded.Add(container); // will want to validate this more
            }

            // what about deletions of containers? conflict?

            // create files
            foreach (var binaryFile in importJob.FilesToAdd)
            {
                binaryFile.Parent = EnsureFedoraPath(binaryFile.Parent);
                logger.LogInformation("Adding file {path}", binaryFile.Path);
                var fedoraBinary = await fedora.PutBinary(binaryFile, transaction);
                logger.LogInformation("Binary created at {location}", fedoraBinary!.Location);
                importJob.FilesAdded.Add(binaryFile);
            }

            // patch files
            // This is EXACTLY the same as Add.
            // We will need to accomodate some RDF updates - but nothing that can't be carried on BinaryFile
            // nothing _arbitrary_
            foreach (var binaryFile in importJob.FilesToPatch)
            {
                binaryFile.Parent = EnsureFedoraPath(binaryFile.Parent);
                logger.LogInformation("Patching file {path}", binaryFile.Path);
                var fedoraBinary = await fedora.PutBinary(binaryFile, transaction);
                logger.LogInformation("Binary PATCHed at {location}", fedoraBinary!.Location);
                importJob.FilesPatched.Add(binaryFile);
            }

            // delete files
            foreach (var binaryFile in importJob.FilesToDelete)
            {
                binaryFile.Parent = EnsureFedoraPath(binaryFile.Parent);
                logger.LogInformation("Deleting file {path}", binaryFile.Path);
                var location = archivalGroup!.GetResourceUri(binaryFile.Path);
                await fedora.Delete(location, transaction);
                logger.LogInformation("Binary DELETEd at {location}", location);
                importJob.FilesDeleted.Add(binaryFile);
            }


            // delete containers
            // Should we verify that the container is empty first?
            // Do we want to allow deletion of non-empty containers? It wouldn't come from a diff importJob
            // but might come from other importJob use.
            foreach (var container in importJob.ContainersToDelete)
            {
                container.Parent = EnsureFedoraPath(container.Parent);
                logger.LogInformation("Deleting container {path}", container.Path);
                var location = archivalGroup!.GetResourceUri(container.Path);
                await fedora.Delete(location, transaction);
                logger.LogInformation("Container DELETEd at {location}", location);
                importJob.ContainersDeleted.Add(container);
            }
        }
        catch(Exception ex)
        {
            logger.LogError(ex, "Caught error in importJob, rolling back transaction");
            await fedora.RollbackTransaction(transaction);
            throw;
        }

        await fedora.CommitTransaction(transaction);

        importJob.End = DateTime.Now;
        return importJob;
    }

    private Uri EnsureFedoraPath(Uri candidate) => candidate.IsAbsoluteUri
        ? candidate
        : new Uri($"{fedoraApiSettings.ApiRoot}{candidate.OriginalString}");
}
