using System.Text.Json;
using Fedora.Abstractions;
using Fedora.Abstractions.Transfer;
using Fedora.Storage;
using Preservation.API.Data.Entities;

namespace Preservation.API.Models;

/// <summary>
/// Easy r-to-l converters for various models
/// </summary>
/// <param name="uriGenerator">Helper for generating various URIs</param>
public class ModelConverter(UriGenerator uriGenerator)
{
    private readonly JsonSerializerOptions settings = new(JsonSerializerDefaults.Web);

    public string GetImportJson(ImportJob importJob) => JsonSerializer.Serialize(importJob, settings);

    public ImportJob GetImportJob(ImportJobEntity importJob) =>
        JsonSerializer.Deserialize<ImportJob>(importJob.ImportJobJson, settings)!;
    
    public PreservationResource ToPreservationResource(Fedora.Abstractions.Resource storageResource, Uri requestPath)
    {
        switch (storageResource)
        {
            case Fedora.Abstractions.ArchivalGroup ag:
            {
                var digitalObject = new DigitalObject
                {
                    Id = requestPath,
                    Name = ag.Name,
                    Version = ToDigitalObjectVersion(ag.Version, requestPath),
                    Versions = (ag.Versions ?? Array.Empty<ObjectVersion>())
                        .Select(v => ToDigitalObjectVersion(v, requestPath)!).ToArray(),
                    Binaries = ag.Binaries.Count == 0 ? null : ag.Binaries.Select(ToPresentationBinary).ToArray(),
                    Containers = ag.Containers.Count == 0
                        ? null
                        : ag.Containers.Select(ToPresentationContainer).ToArray(),
                };
                MapBasicsFromStorageResource(digitalObject, storageResource);
                return digitalObject;
            }
            case Fedora.Abstractions.Container c:
                return ToPresentationContainer(c);
            case Fedora.Abstractions.Binary b:
                return ToPresentationBinary(b);
        }

        throw new InvalidOperationException($"Unable to handle {storageResource.GetType()} resource");
    }

    public Deposit ToDeposit(DepositEntity entity) =>
        new()
        {
            Id = uriGenerator.GetDepositPath(entity.Id),
            Status = entity.Status,
            SubmissionText = entity.SubmissionText,
            DigitalObject = entity.PreservationPath,
            Files = entity.S3Root.ToString(),
            Created = entity.Created,
            CreatedBy = new Uri($"http://example.id/{entity.CreatedBy}"),
            LastModified = entity.LastModified,
            LastModifiedBy = string.IsNullOrEmpty(entity.LastModifiedBy)
                ? null
                : new Uri($"http://example.id/{entity.LastModifiedBy}"),
        };

    public ImportJobResult ToImportJobResult(ImportJobEntity entity) =>
        new()
        {
            Id = uriGenerator.GetImportJobResultUri(entity.Deposit, entity.Id),
            Created = entity.DateSubmitted ?? DateTime.MinValue,
            CreatedBy = new Uri($"http://example.id/todo"),
            DigitalObject = entity.DigitalObject,
            Deposit = uriGenerator.GetDepositPath(entity.Deposit),
            OriginalImportJobId = entity.OriginalImportJobId,
            Status = entity.Status,
            Errors = string.IsNullOrEmpty(entity.Errors) ? null : JsonSerializer.Deserialize<Error[]>(entity.Errors),
            ContainersAdded = string.IsNullOrEmpty(entity.ContainersAdded)
                ? Array.Empty<Container>()
                : JsonSerializer.Deserialize<Container[]>(entity.ContainersAdded)!,
            ContainersDeleted = string.IsNullOrEmpty(entity.ContainersDeleted)
                ? Array.Empty<Container>()
                : JsonSerializer.Deserialize<Container[]>(entity.ContainersDeleted)!,
            BinariesAdded = string.IsNullOrEmpty(entity.BinariesAdded)
                ? Array.Empty<Binary>()
                : JsonSerializer.Deserialize<Binary[]>(entity.BinariesAdded)!,
            BinariesDeleted = string.IsNullOrEmpty(entity.BinariesDeleted)
                ? Array.Empty<Binary>()
                : JsonSerializer.Deserialize<Binary[]>(entity.BinariesDeleted)!,
            BinariesPatched = string.IsNullOrEmpty(entity.BinariesPatched)
                ? Array.Empty<Binary>()
                : JsonSerializer.Deserialize<Binary[]>(entity.BinariesPatched)!,
        };

    /*public PreservationImportJob ToPreservationResource(ImportJob importJob) //, Uri deposit)
        => new()
        {
            DigitalObject = importJob.ArchivalGroupUri!,
            Created = importJob.DiffStart,
            CreatedBy = new Uri($"http://example.id/need-to-set"),
            BinariesToAdd = importJob.FilesToAdd.Select(f => ToPresentationBinary(f)).ToArray(),
            BinariesToDelete = importJob.FilesToDelete.Select(f => ToPresentationBinary(f)).ToArray(),
            BinariesToPatch = importJob.FilesToPatch.Select(f => ToPresentationBinary(f)).ToArray(),
            ContainersToDelete = importJob.ContainersToDelete.Select(c => ToPresentationContainer(c)).ToArray(),
            ContainersToAdd = importJob.ContainersToAdd.Select(c => ToPresentationContainer(c)).ToArray(),
        };

    public ImportJobEntity ToEntity(PreservationImportJob preservationImportJob, string id) =>
        new()
        {
            Id = id,
            DigitalObject = preservationImportJob.DigitalObject,
            ImportJobJson = JsonSerializer.Serialize(preservationImportJob),
            OriginalImportJobId = preservationImportJob.Id!,
        };*/

    private DigitalObjectVersion? ToDigitalObjectVersion(ObjectVersion? objectVersion, Uri repositoryUri)
    {
        if (objectVersion == null) return null;

        var digitalObjectVersion = new DigitalObjectVersion
        {
            Id = uriGenerator.GetRepositoryPath(repositoryUri, objectVersion.OcflVersion),
            Name = objectVersion.OcflVersion,
            Date = objectVersion.MementoDateTime,
        };
        return digitalObjectVersion;
    }
    
    private Binary ToPresentationBinary(Fedora.Abstractions.Binary fedoraBinary)
    {
        var binary = new Binary
        {
            Id = uriGenerator.GetRepositoryPath(fedoraBinary.StorageApiUri),
            Content = new Uri("https://todo"),
            Name = fedoraBinary.FileName,
            Digest = fedoraBinary.Digest,
            PartOf = uriGenerator.GetRepositoryPath(fedoraBinary.PartOf),
        };

        MapBasicsFromStorageResource(binary, fedoraBinary);
        return binary;
    }

    public Binary ToPresentationBinary(BinaryFile binaryFile) =>
        new()
        {
            Id = uriGenerator.GetRepositoryPath(binaryFile.Path), // is this right? Should differ from below?
            Content = uriGenerator.GetRepositoryPath(binaryFile.Path),
            Name = binaryFile.Name,
            Digest = binaryFile.Digest,
            PartOf = uriGenerator.GetRepositoryPath(binaryFile.Parent)
        };

    public Container ToPresentationContainer(ContainerDirectory containerDirectory) =>
        new()
        {
            Id = uriGenerator.GetRepositoryPath(containerDirectory.Path),
            Name = containerDirectory.Name,
            PartOf = uriGenerator.GetRepositoryPath(containerDirectory.Parent),
        };

    private Container ToPresentationContainer(Fedora.Abstractions.Container fedoraContainer)
    {
        var container = new Container
        {
            Id = uriGenerator.GetRepositoryPath(fedoraContainer.StorageApiUri),
            Name = fedoraContainer.Name,
            Containers = fedoraContainer.Containers.Count == 0
                ? null
                : fedoraContainer.Containers.Select(ToPresentationContainer).ToArray(),
            Binaries = fedoraContainer.Binaries.Count == 0
                ? null
                : fedoraContainer.Binaries.Select(ToPresentationBinary).ToArray(),
            PartOf = uriGenerator.GetRepositoryPath(fedoraContainer.PartOf),
        };
        
        MapBasicsFromStorageResource(container, fedoraContainer);
        return container;
    }
    
    private void MapBasicsFromStorageResource<T>(T target, Resource resource)
        where T : PreservationResource, new()
    {
        target.Created = resource.Created ?? DateTime.MinValue;
        target.CreatedBy = new Uri($"http://example.id/{resource.CreatedBy}");
        target.LastModified = resource.LastModified;
        target.LastModifiedBy = new Uri($"http://example.id/{resource.LastModifiedBy}");
    }
}