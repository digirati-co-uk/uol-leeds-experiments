using Fedora.Abstractions.Transfer;
using System.Text.Json.Serialization;

namespace Preservation;

public class ImportSource
{
    [JsonPropertyName("containers")]
    [JsonPropertyOrder(1)]
    public List<ContainerDirectory> Containers { get; set; } = [];

    /// <summary>
    /// Fedora binaries that need to be created to synchronise the Archival Group object with the source
    /// </summary>
    [JsonPropertyName("files")]
    [JsonPropertyOrder(2)]
    public List<BinaryFile> Files { get; set; } = [];
}
