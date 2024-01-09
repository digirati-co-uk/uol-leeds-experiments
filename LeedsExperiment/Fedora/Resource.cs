using Fedora.ApiModel;
using System.Text.Json.Serialization;

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

    [JsonPropertyName("@id")]
    [JsonPropertyOrder(1)]
    // The URI for this API
    public Uri? PreservationApiUri { get; set; }

    [JsonPropertyName("type")]
    [JsonPropertyOrder(1)]
    public string? Type { get; set; }

    [JsonPropertyName("origin")]
    [JsonPropertyOrder(4)]
    public string? Origin { get; set; }

    [JsonPropertyName("name")]
    [JsonPropertyOrder(11)]
    // The original name of the resource (possibly non-filesystem-safe)
    // Use dc:title on the fedora resource
    public string? Name { get; set; }

    [JsonPropertyName("id")]
    [JsonPropertyOrder(12)]
    // The Fedora identifier
    public Uri Location { get; set; }

    [JsonPropertyName("created")]
    [JsonPropertyOrder(13)]
    public DateTime? Created { get; set; }

    [JsonPropertyName("createdBy")]
    [JsonPropertyOrder(14)]
    public string? CreatedBy { get; set; }

    [JsonPropertyName("lastModified")]
    [JsonPropertyOrder(15)]
    public DateTime? LastModified { get; set; }

    [JsonPropertyName("lastModifiedBy")]
    [JsonPropertyOrder(16)]
    public string? LastModifiedBy { get; set; }
}
