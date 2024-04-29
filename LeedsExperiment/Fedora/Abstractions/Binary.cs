using Fedora.ApiModel;
using System.Text.Json.Serialization;

namespace Fedora.Abstractions;

public class Binary : Resource
{
    public Binary() { }

    public Binary(FedoraJsonLdResponse jsonLdResponse) : base(jsonLdResponse)
    {
        Type = "Binary";
        var binaryresp = jsonLdResponse as BinaryMetadataResponse;
        if (binaryresp != null)
        {
            FileName = binaryresp.FileName;
            ContentType = binaryresp.ContentType;
            Size = Convert.ToInt64(binaryresp.Size);
            Digest = binaryresp.Digest?.Split(':')[^1];
        }
    }

    /// <summary>
    /// The ebucore:filename triple in Fedora.
    /// This is here for visibility, but we will not use it in the Storage API - use Name to hold original file name.
    /// 
    /// We could validate that it's always the same as Name
    /// </summary>
    [JsonIgnore]
    public string? FileName { get; set; }

    [JsonPropertyName("contentType")]
    [JsonPropertyOrder(22)]
    public string? ContentType { get; set; }

    [JsonPropertyName("size")]
    [JsonPropertyOrder(23)]
    public long Size { get; set; }

    [JsonPropertyName("digest")]
    [JsonPropertyOrder(24)]
    public string? Digest { get; set; }

    public override string ToString()
    {
        return $"🗎 {Name ?? GetType().Name}";
    }
}
