using Fedora.ApiModel;

namespace Fedora;

public abstract class Resource
{
    public Resource(FedoraJsonLdResponse jsonLdResponse)
    {
        Location = jsonLdResponse.Id;
        Name = jsonLdResponse.Title;
        Created = jsonLdResponse.Created;
        CreatedBy = jsonLdResponse.CreatedBy;
        LastModified = jsonLdResponse.LastModified;
        LastModifiedBy = jsonLdResponse.LastModifiedBy;
    }

    // The original name of the resource (possibly non-filesystem-safe)
    // Use dc:title on the fedora resource
    public string? Name { get; set; }

    // The Fedora identifier
    public required Uri Location { get; set; }

    public DateTime? Created { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? LastModified { get; set; }
    public string? LastModifiedBy { get; set; }
}
