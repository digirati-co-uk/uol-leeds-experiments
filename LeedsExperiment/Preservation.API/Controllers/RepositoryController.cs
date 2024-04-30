using Fedora.Abstractions;
using Fedora.Storage;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace Preservation.API.Controllers;

[Route("[controller]/{*path}")]
[ApiController]
public class RepositoryController(IPreservation preservation) : Controller
{
    /// <summary>
    /// Browse underlying repository for Container, DigitalObject or Binary.
    /// </summary>
    /// <returns></returns>
    [HttpGet]
    public async Task<IActionResult> Index([FromRoute] string path, [FromQuery] string? version = null)
    {
        // How do we know if this is an archive group or not?
        var storageResource = string.IsNullOrEmpty(version)
            ? await preservation.GetResource(path)
            : await preservation.GetArchivalGroup(path, version);

        if (storageResource == null) return NotFound();
        
        // Convert to appropriate type
        var preservationResource = storageResource.ToPreservationResource(new Uri(HttpContext.Request.GetDisplayUrl()));
        return Ok(preservationResource);
    }
}

public static class ModelConverters
{
    public static PreservationResource ToPreservationResource(this Resource storageResource, Uri requestPath)
    {
        if (storageResource is ArchivalGroup ag)
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

        throw new NotImplementedException("Only AG handled");
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