﻿using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Util;
using Amazon.Util;
using Fedora.Abstractions;
using Fedora.Abstractions.Transfer;
using Fedora.Storage;
using Microsoft.Extensions.Options;
using Utils;

namespace Storage;

public interface IImportService
{
    // passing diffStart feels off, this should be able to do work it out _but_ we do want to know how long the AG fetch
    // etc took too. Maybe need a .Start() operation that returns an IDisposable that does the work?? Is that overkill?
    /*
     using var importer = importSvc.Start();
     var ag = FetchAg(uri);
     importer.DiffWithS3(ag, s3Uri);
     importer.EmbellishFromMets(ag, metsLocation);
     var importJob = importer.GenerateJob();
     */
    Task<ImportJob> GetImportJob(
            ArchivalGroup? archivalGroup, 
            Uri sourceUri, 
            Uri archivalGroupUri,
            DateTime diffStart,
            bool errorIfMissingS3Checksum,
            bool embellishFromMets);


    /// <summary>
    /// Return information about an input source (e.g., S3 location), usually before you try to create an ImportJob from it
    /// </summary>
    /// <param name="sourceUri"></param>
    /// <returns></returns>
    Task<ImportSource> GetImportSource(Uri sourceUri);

    /// <summary>
    /// Copy the contents of sourceUri to a new location, this time with checksums.
    /// </summary>
    /// <param name="sourceUri"></param>
    /// <returns>The Source property will be the new Uri</returns>
    Task<ImportSource> CopyToNewSourceWithChecksums(Uri sourceUri);

    // Task EmbellishFromMets(ImportJob importJob, ArchivalGroup? existingArchivalGroup);
}

/// <summary>
/// Service responsible for generating an <see cref="ImportJob"/> that can be understood by the storage-api using
/// S3 location as source for change.
/// </summary>
public class S3ImportService : IImportService
{
    private IAmazonS3 s3Client;

    public S3ImportService(
        IAmazonS3 s3Client,
        IOptions<FedoraApiOptions> fedoraApiOptions)
    {
        this.s3Client = s3Client;
    }

    private readonly FileExtensionContentTypeProvider contentTypeProvider = new();

    public async Task<ImportJob> GetImportJob(
        ArchivalGroup? archivalGroup, 
        Uri sourceUri, 
        Uri archivalGroupUri,
        DateTime diffStart, 
        bool errorIfMissingS3Checksum,
        bool embellishFromMets
        ) // passing diffStart feels off, this should be able to do it here
    {
        // TODO - how does PreservationAPI calling this know the archivalGroupUri as that's the Fedora URI?

        // And also, see below, this needs to know fedoraApiSettings.ApiRoot to strip the Uri to get the relative path.

        // I think it should just take:
        //
        // ArchivalGroup? archivalGroup,
        // Uri sourceUri, 
        // string archivalGroupPath - e.g., my-stuff/my-ag


        // don't pass true as third param
        // identify METS as we traverse? Or before
        // mets = GetMetsInfo(sourceUri) (object representing just the things we want, that could also be our JSON format)
        // container structure with names... can these be containers and binaries too?
        // get mets s3 key so we can forgive its lack of S3
        // pass in metsInfo and use 
        var importSource = await GetImportSource(sourceUri, archivalGroupUri, errorIfMissingS3Checksum, embellishFromMets);
        
        var importJob = new ImportJob
        {
            ArchivalGroupUri = archivalGroupUri,
            StorageType = StorageTypes.S3, // all we support for now
            ArchivalGroupPath = Utils.ArchivalGroupUriHelpers.GetArchivalGroupPath(archivalGroupUri), 
            Source = sourceUri.ToString(),
            DiffStart = diffStart
        };

        if (importSource.Name.HasText())
        {
            importJob.ArchivalGroupName = importSource.Name;
        }
        
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

        importJob.DiffEnd = DateTime.UtcNow;
        return importJob;
    }

    public async Task<ImportSource> GetImportSource(Uri source)
    {
        // yuk - didn't want to make BinaryFile.Parent nullable so am passing this dummy for now
        // But maybe it should be nullable - it can represent a file in S3 that doesn't yet know
        // where it's going.
        //                                               !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
        var importSource = await GetImportSource(source, new Uri("https://example.org/parent"), false, false);
        return importSource;
    }

    public async Task<ImportSource> CopyToNewSourceWithChecksums(Uri sourceUri)
    {
        var existingSource = await GetImportSource(sourceUri);
        // we're just going to create a copy of the source alongside the old one, and not worry about unlikely collisions
        var existingStr = sourceUri.ToString();
        const string suffix = "-CHK";
        string? newStr;
        if (existingStr[^1] == '/')
        {
            newStr = $"{existingStr.Remove(existingStr.Length - 1)}{suffix}/";
        }
        else
        {
            newStr = $"{existingStr}{suffix}";
        }

        foreach (var file in existingSource.Files)
        {
            var existingFileUri = new AmazonS3Uri(file.ExternalLocation);
            var newFileUri = new AmazonS3Uri(file.ExternalLocation.Replace(existingStr, newStr));
            if(existingFileUri.Bucket != newFileUri.Bucket)
            {
                throw new Exception("Unexpected bucket mismatch");
            }
            var req = new CopyObjectRequest
            {
                SourceBucket = existingFileUri.Bucket,
                SourceKey = existingFileUri.Key,
                DestinationBucket = newFileUri.Bucket,
                DestinationKey = newFileUri.Key,
                ChecksumAlgorithm = ChecksumAlgorithm.SHA256
            };
            await s3Client.CopyObjectAsync(req);
        }

        var newSource = await GetImportSource(new Uri(newStr));
        return newSource;
    }


    // Generate an ImportSource for S3 - if we want to support alternative means of diff-generation (e.g. METS)
    // then would it just be a case of having alternative implementations of this?
    private async Task<ImportSource> GetImportSource(Uri source, Uri? intendedParent, bool errorIfMissingChecksum, bool embellishFromMets)
    {
        // NOTE - this is refactored from Storage ImportController to common class for use by Preservation
        
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

        var importSource = new ImportSource { Source = source };
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
            if (string.IsNullOrWhiteSpace(sha256) && errorIfMissingChecksum)
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

        foreach (string containerPath in containerPaths)
        {
            var nameAndParentPath = new NameAndParentPath(containerPath);
            importSource.Containers.Add(new ContainerDirectory
            {
                Name = nameAndParentPath.Name,
                Parent = intendedParent,
                Path = containerPath
            });
        }

        if(embellishFromMets)
        {
            await EmbellishFromMets(importSource);
        }

        return importSource;
    }
    
    private void PopulateDiffTasks(ArchivalGroup archivalGroup, ImportSource importSource, ImportJob importJob)
    {
        // What's the best way to diff?
        // This is very crude and can't spot a container being renamed

        // see https://universityofleeds.visualstudio.com/Library/_backlogs/backlog/DLIP/Epics?workitem=77035

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
            if (string.IsNullOrEmpty(existingFile.Digest) || string.IsNullOrEmpty(importFile.Digest))
            {
                throw new Exception("Missing digest in diff operation for " + existingFile.Path);
            }

            if (existingFile.Digest != importFile.Digest)
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

    private string GetDefaultContentType(string path)
    {
        const string defaultContentType = "application/octet-stream";
        if (!contentTypeProvider.TryGetContentType(path, out string? contentType))
        {
            contentType = defaultContentType;
        }
        return contentType!;
    }

    public async Task EmbellishFromMets(ImportSource importSource)
    {
        var metsParser = new MetsParser.Parser(s3Client);
        var metsFile = await metsParser.ResolveAndParseAsync(importSource.Source);
        if(metsFile.Self == null)
        {
            // no mets file was found
            // log this? treat as error?
            return;
        }

        // (the existing Archival Group is fully populated - all files and folders, recurse=true)


        // we might need to 
        // take checksums from the mets file and add them to the import job
        // validate the import job against the mets file - do they agree? Are the files where they say they are?
        // update the `name` properties of files in the import job
        // we might also need to add new tasks to the import job for containers and files that just have `name` changes (TODO - not in prototype)

        // First pass, just embellish the import job from METS without looking at the existingArchivalGroup

        foreach (var bf in importSource.Files)
        {
            var fileInMets = metsFile.Files.SingleOrDefault(f => f.Path == bf.Path);
            if(fileInMets != null)
            {
                if (fileInMets.Digest.HasText())
                {
                    bf.Digest = fileInMets.Digest;
                }
                if (fileInMets.Name.HasText())
                {
                    bf.Name = fileInMets.Name;
                }
            }
        }

        foreach(var cd in importSource.Containers)
        {
            var containerInMets = metsFile.Directories.SingleOrDefault(d => d.Path == cd.Path);
            if(containerInMets != null)
            {
                if (containerInMets.Name.HasText())
                {
                    cd.Name = containerInMets.Name;
                }
            }
        }

        if(metsFile.Name.HasText())
        {
            importSource.Name = metsFile.Name;
        }
    }
}