using Fedora.Abstractions;
using Fedora.Storage;

namespace Preservation.API.Models;

public class ModelConverter(UriGenerator uriGenerator)
{
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
                    Binaries = ag.Binaries.Select(b => ToPresentationBinary(b)).ToArray(),
                    Containers = ag.Containers.Select(c => ToPresentationContainer(c)).ToArray(),
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
            Location = new Uri("s3://todo/path"),
            PartOf = uriGenerator.GetRepositoryPath(fedoraBinary.PartOf),
        };

        MapBasicsFromStorageResource(binary, fedoraBinary);
        return binary;
    }

    private Container ToPresentationContainer(Fedora.Abstractions.Container fedoraContainer)
    {
        var container = new Container
        {
            Id = uriGenerator.GetRepositoryPath(fedoraContainer.StorageApiUri),
            Name = fedoraContainer.Name,
            Containers = fedoraContainer.Containers.Select(c => ToPresentationContainer(c)).ToArray(),
            Binaries = fedoraContainer.Binaries.Select(b => ToPresentationBinary(b)).ToArray(),
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