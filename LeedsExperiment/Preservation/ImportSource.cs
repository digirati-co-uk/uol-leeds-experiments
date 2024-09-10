using Fedora.Abstractions.Transfer;
using System.Text.Json.Serialization;

namespace Storage;

public class ImportSource
{
    public Uri Source { get; set; }

    // If the source itself can supply a name: typically mods:title from a METS file
    public string? Name { get; set; }

    [JsonPropertyName("containers")]
    [JsonPropertyOrder(1)]
    public List<ContainerDirectory> Containers { get; set; } = [];

    /// <summary>
    /// Binaries that need to be created to synchronise the Archival Group object with the source
    /// </summary>
    [JsonPropertyName("files")]
    [JsonPropertyOrder(2)]
    public List<BinaryFile> Files { get; set; } = [];
}
