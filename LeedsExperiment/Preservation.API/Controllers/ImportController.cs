using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Util;
using Fedora;
using Fedora.Abstractions;
using Fedora.Abstractions.Transfer;
using Fedora.ApiModel;
using Fedora.Storage;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;

namespace Preservation.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ImportController : Controller
{
    private readonly IFedora fedora;
    private IAmazonS3 s3Client;
    private FileExtensionContentTypeProvider contentTypeProvider = new FileExtensionContentTypeProvider();
    private ILogger<ImportController> logger;

    public ImportController(
        IFedora fedora,
        IAmazonS3 awsS3Client,
        ILogger<ImportController> logger
    )
    {
        this.fedora = fedora;
        this.s3Client = awsS3Client;
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
        var diffStart = DateTime.Now;

        ArchivalGroup? archivalGroup;
        archivalGroup = await GetValidatedArchivalGroupForImportJob(agUri);

        // This is either an existing Archival Group, or a 404 where the immediate parent is a Container that is not itself part of an Archival Group.
        // So now evaluate the source:

        var importSource = await GetImportSource(source, agUri);
        var importJob = new ImportJob
        {
            ArchivalGroupUri = agUri,
            StorageType = StorageTypes.S3,  // all we support for now
            ArchivalGroupPath = archivalGroupPath,
            Source = source,
            DiffStart = diffStart
        };
        if (archivalGroup == null)
        {
            // This is a new object
            importJob.ContainersToAdd = importSource.Containers;
            importJob.FilesToAdd = importSource.Files;
        }
        else
        {
            importJob.ArchivalGroupName = archivalGroup.Name;
            importJob.IsUpdate = true;
            importJob.DiffVersion = archivalGroup.Version;
            PopulateDiffTasks(archivalGroup, importSource, importJob);
        }
        importJob.DiffEnd = DateTime.Now;
        return importJob;
    }

    /// <summary>
    /// Returns an archivalGroup if there is one at archivalGroupUri
    /// Returns null if there isn't one there
    /// Throws an exception if it's not possible to _create_ an archival group there.
    /// </summary>
    /// <param name="archivalGroupUri"></param>
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
            var npp = new NameAndParentPath(archivalGroupUri.AbsolutePath);
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

    private void PopulateDiffTasks(ArchivalGroup archivalGroup, ImportSource importSource, ImportJob importJob)
    {
        // What's the best way to diff?
        // This is very crude and can't spot a container being renamed

        var allExistingContainers = new List<ContainerDirectory>();
        var allExistingFiles = new List<BinaryFile>();
        TraverseContainers(archivalGroup, allExistingContainers, allExistingFiles, archivalGroup);

        importJob.FilesToAdd.AddRange(importSource.Files.Where(
            importFile => !allExistingFiles.Exists(existingFile => existingFile.Path == importFile.Path)));
        
        importJob.FilesToDelete.AddRange(allExistingFiles.Where(
            existingFile => !importSource.Files.Exists(importFile => importFile.Path == existingFile.Path)));
        
        foreach(var importFile in importSource.Files.Where(
            importFile => !importJob.FilesToAdd.Exists(f => f.Path == importFile.Path)))
        {
            // files not already put in FilesToAdd
            var existingFile = allExistingFiles.Single(existingFile => existingFile.Path == importFile.Path);
            if(string.IsNullOrEmpty(existingFile.Digest) || string.IsNullOrEmpty(importFile.Digest))
            {
                throw new Exception("Missing digest in diff operation for " + existingFile.Path);
            }
            if(existingFile.Digest != importFile.Digest)
            {
                importJob.FilesToPatch.Add(importFile);
            }
        }

        importJob.ContainersToAdd.AddRange(importSource.Containers.Where(
            importContainer => !allExistingContainers.Exists(existingContainer => existingContainer.Path == importContainer.Path)));

        importJob.ContainersToDelete.AddRange(allExistingContainers.Where(
            existingContainer => !importSource.Containers.Exists(importContainer => importContainer.Path == existingContainer.Path)));

        // Later we will also need patch ops on container (for data)
        // and patch ops on file for metadata as well as digest difference as above.
    }

    private static void TraverseContainers(
        ArchivalGroup archivalGroup, 
        List<ContainerDirectory> allExistingContainers, 
        List<BinaryFile> allExistingFiles, 
        Container traverseContainer)
    {
        foreach (Container container in traverseContainer.Containers)
        {
            allExistingContainers.Add(new ContainerDirectory
            {
                Name = container.Name!,
                Parent = archivalGroup.Location!,
                Path = container.ObjectPath!.Remove(0, archivalGroup.ObjectPath!.Length + 1)
            });
            TraverseContainers(archivalGroup, allExistingContainers, allExistingFiles, container);
        }
        foreach (Binary binary in traverseContainer.Binaries)
        {
            allExistingFiles.Add(new BinaryFile
            {
                Name = binary.Name!,
                Parent = archivalGroup.Location!,
                Path = binary.ObjectPath!.Remove(0, archivalGroup.ObjectPath!.Length + 1),
                ContentType = binary.ContentType ?? string.Empty,
                StorageType = StorageTypes.S3, // shouldn't have to hard code that here, but Binary doesn't have that prop
                Digest = binary.Digest,
                // FileName = binary.FileName!,
                ExternalLocation = binary.Origin ?? string.Empty // we won't use this because it's the destination                
            });
        }
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
                logger.LogInformation("Creating container {path}", container.Path);
                var fedoraContainer = await fedora.CreateContainer(container, transaction);
                logger.LogInformation("Container created at {location}", fedoraContainer!.Location);
                importJob.ContainersAdded.Add(container); // will want to validate this more
            }

            // what about deletions of containers? conflict?

            // create files
            foreach (var binaryFile in importJob.FilesToAdd)
            {
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
                logger.LogInformation("Patching file {path}", binaryFile.Path);
                var fedoraBinary = await fedora.PutBinary(binaryFile, transaction);
                logger.LogInformation("Binary PATCHed at {location}", fedoraBinary!.Location);
                importJob.FilesPatched.Add(binaryFile);
            }

            // delete files
            foreach (var binaryFile in importJob.FilesToDelete)
            {
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


    private async Task<ImportSource> GetImportSource(string source, Uri intendedParent)
    {
        // Move this behind an interface later
        // This will currently break if the source is not an s3 Uri to which we have access
        // but later could be a file path etc, a scratch upload location, whatever
        var s3Uri = new AmazonS3Uri(source);

        // FOR THIS DEMO we assume this is the root.
        // We also assume that we are not going to hit the AWS limit for paging (1000?)
        // https://docs.aws.amazon.com/sdkfornet1/latest/apidocs/html/M_Amazon_S3_AmazonS3_ListObjects.htm
        // We can't learn anything about containers this way other than that there are slugs in path
        // We can't learn anything about intended name (dc:title) from this, but that's OK for now
        // That kind of data should be in METS files; we can enhance the ImportJob with it later in a real world application
        // The code that constructs the import job has access to more information than the code below.
        // The files have been through a pipeline that will have produced checksums, content types and more, and put them in
        // metadata such as METS that that code understands.
        var listObjectsReq = new ListObjectsV2Request()
        { 
            BucketName = s3Uri.Bucket,
            Prefix = $"{s3Uri.Key.TrimEnd('/')}/"

            // The only valid values here are RestoreStatus or null, so this is no good
            // https://sdk.amazonaws.com/java/api/latest/software/amazon/awssdk/services/s3/model/OptionalObjectAttributes.html
            // OptionalObjectAttributes = [ObjectAttributes.Checksum] //,
            // OptionalObjectAttributes = ["Content-Type"] - need to work out how to get content type back here
            // https://stackoverflow.com/a/44179929
            // application/x-directory
        };

        var importSource = new ImportSource();
        var response = await s3Client.ListObjectsV2Async(listObjectsReq);
        var containerPaths = new HashSet<string>();
        foreach (S3Object obj in response.S3Objects)
        {
            if(obj.Key.EndsWith('/') && obj.Size == 0)
            {
                // This is an AWS "folder" - but we can have a better check than this -
                // also see if it's application/x-directory
                continue;
            }

            // how do you get the checksum here without making a further call?

            // S3 source folders either need SHA-256 hashes in their AWS metadata (preferred for package-building)
            // or they are recorded in things like METS files - in a way that this code here can understand.

            // Different applications have their own logic for storing hashes as part of the object, e.g., in METS.

            // Unless coming from other information, we *require* that S3 source folders have sha256 hashes in their metadata
            // (how do we enforce that, we don't want to know about METS here)

            // Get the SHA256 algorithm from AWS directly rather than compute it here
            // If the S3 file does not already have the SHA-256 in metadata, then it's an error
            string? sha256 = await AwsChecksum.GetHexChecksumAsync(s3Client, s3Uri.Bucket, obj.Key);
            if (string.IsNullOrWhiteSpace(sha256))
            {
                throw new InvalidOperationException($"S3 Key at {obj.Key} does not have SHA256 Checksum in its attributes");
            }
                       

            // so we don't have to do this:
            // var s3Stream = await s3Client!.GetObjectStreamAsync(obj.BucketName, obj.Key, null);
            // var sha256Digest = Checksum.Sha256FromStream(s3Stream);
            
            // We can also do an eTag comparison for smaller files
            // We can also do a size comparison as a sanity check - this can't catch all changes obvs
            // but if source and current have same checksum but different sizes then something's up

            var sourcePath = obj.Key.Remove(0, listObjectsReq.Prefix.Length);
            var nameAndParentPath = new NameAndParentPath(sourcePath);
            if(nameAndParentPath.ParentPath != null)
            {
                containerPaths.Add(nameAndParentPath.ParentPath);
            }
            importSource.Files.Add(new BinaryFile
            {
                Name = nameAndParentPath.Name,
                // FileName = nameAndParentPath.Name,
                Parent = intendedParent,
                Path = sourcePath,
                StorageType = StorageTypes.S3,
                ExternalLocation = $"s3://{obj.BucketName}/{obj.Key}",
                Digest = sha256,
                ContentType = GetDefaultContentType(nameAndParentPath.Name) // we may overwrite this later, e.g., from PREMIS data
            });
        }
        foreach(string containerPath in containerPaths)
        {
            var nameAndParentPath = new NameAndParentPath(containerPath);
            importSource.Containers.Add(new ContainerDirectory
            {
                Name = nameAndParentPath.Name,
                Parent = intendedParent,
                Path = containerPath
            });
        }
        return importSource;
    }

    private string GetDefaultContentType(string path)
    {
        const string DefaultContentType = "application/octet-stream";
        if (!contentTypeProvider.TryGetContentType(path, out string? contentType))
        {
            contentType = DefaultContentType;
        }
        return contentType;
    }
}
