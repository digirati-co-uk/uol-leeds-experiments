using Fedora.Abstractions;
using Fedora.Storage;

namespace Preservation.API;

public static class ModelConverters
{
    public static PreservationResource ToPreservationResource(this Fedora.Abstractions.Resource storageResource, Uri requestPath)
    {
        switch (storageResource)
        {
            case Fedora.Abstractions.ArchivalGroup ag:
            {
                var digitalObject = new DigitalObject
                {
                    Id = requestPath, // TODO remove version query param
                    Name = ag.Name,
                    Version = ag.Version.ToDigitalObjectVersion(requestPath),
                    Versions = (ag.Versions ?? Array.Empty<ObjectVersion>())
                        .Select(v => v.ToDigitalObjectVersion(requestPath)!).ToArray(),
                    Binaries = ag.Binaries.Select(b => b.ToPresentationBinary()).ToArray(),
                    Containers = ag.Containers.Select(c => c.ToPresentationContainer()).ToArray(),
                };
                digitalObject.MapBasicsFromStorageResource(storageResource);
                return digitalObject;
            }
            case Fedora.Abstractions.Container c:
                return c.ToPresentationContainer();
            case Fedora.Abstractions.Binary b:
                return b.ToPresentationBinary();
        }

        throw new InvalidOperationException($"Unable to handle {storageResource.GetType()} resource");
    }

    private static DigitalObjectVersion? ToDigitalObjectVersion(this ObjectVersion? objectVersion, Uri repositoryUri)
    {
        if (objectVersion == null) return null;

        var objectId = new UriBuilder(repositoryUri);
        if (!string.IsNullOrEmpty(objectVersion.OcflVersion))
        {
            objectId.Query = $"?version={objectVersion.OcflVersion}";
        }

        var digitalObjectVersion = new DigitalObjectVersion
        {
            Id = objectId.Uri,
            Name = objectVersion.OcflVersion,
            Date = objectVersion.MementoDateTime,
        };
        return digitalObjectVersion;
    }
    
    private static Binary ToPresentationBinary(this Fedora.Abstractions.Binary fedoraBinary)
    {
        var binary = new Binary
        {
            Id = new Uri("http://todo.uri"),
            Content = new Uri("http://todo.uri"),
            Name = fedoraBinary.FileName,
            Digest = fedoraBinary.Digest,
            Location = new Uri("s3://todo/path"),
            PartOf = new Uri("http://todo.uri"),
        };

        binary.MapBasicsFromStorageResource(fedoraBinary);
        return binary;
    }

    private static Container ToPresentationContainer(this Fedora.Abstractions.Container fedoraContainer)
    {
        var container = new Container
        {
            Name = fedoraContainer.Name,
            Containers = fedoraContainer.Containers.Select(c => c.ToPresentationContainer()).ToArray(),
            Binaries = fedoraContainer.Binaries.Select(b => b.ToPresentationBinary()).ToArray(),
            PartOf = new Uri("http://todo.uri"),
        };
        
        container.MapBasicsFromStorageResource(fedoraContainer);
        return container;
    }
    
    private static void MapBasicsFromStorageResource<T>(this T target, Resource resource)
        where T : PreservationResource, new()
    {
        target.Created = resource.Created ?? DateTime.MinValue;
        target.CreatedBy = new Uri($"http://example.id/{resource.CreatedBy}");
        target.LastModified = resource.LastModified;
        target.LastModifiedBy = new Uri($"http://example.id/{resource.LastModifiedBy}");
    }
}