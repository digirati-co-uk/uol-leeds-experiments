using System.Text.Json.Serialization;

namespace Fedora.Abstractions.Transfer;

/// <summary>
/// Used when importing new files into the repository from a source location (disk or S3)
/// or exporting to a location
/// </summary>
public class BinaryFile : ResourceWithParentUri
{
    /// <summary>
    /// An S3 key, a filesystem path - somewhere accessible to the Preservation API, to import from or export to
    /// </summary>    
    [JsonPropertyName("externalLocation")]
    [JsonPropertyOrder(11)]
    public required string ExternalLocation { get; set; }

    [JsonPropertyName("storageType")]
    [JsonPropertyOrder(12)]
    public required string StorageType { get; set; }

    /// <summary>
    /// The Original / actual name of the file, rather than the path-safe, reduced character set slug.
    /// Goes into Fedora as Content-Disposition
    /// </summary>
    [JsonPropertyName("fileName")]
    [JsonPropertyOrder(13)]
    public required string FileName { get; set; }

    // NB ^^^ for a filename like readme.txt, Slug, Name and FileName will all be the same.
    // And in practice, Name and FileName are going ot be the same
    // But Slug may differ as it always must be in the reduced character set

    /// <summary>
    /// We do require this but it might sometimes be difficult for a client to supply it.
    /// It should not be required on an update
    /// </summary>
    [JsonPropertyName("contentType")]
    [JsonPropertyOrder(14)]
    public required string ContentType { get; set; }

    [JsonPropertyName("digest")]
    [JsonPropertyOrder(15)]
    public string? Digest { get; set; }
}
