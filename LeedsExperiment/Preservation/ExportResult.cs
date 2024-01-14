
using Fedora.Abstractions.Transfer;
using Fedora.Storage;

namespace Preservation;

public class ExportResult
{
    /// <summary>
    ///  For info - the path of the source archival group
    /// </summary>
    public required string Path { get; set; }

    /// <summary>
    /// The version that was exported
    /// </summary>
    public required ObjectVersion Version { get; set; }
    public DateTime Start { get; set; }
    public DateTime End { get; set; }

    // The root location (S3 Uri, directory path) where the ArchivalGroup has been exported
    public required string Source { get; set; }
    public required string StorageType { get; set; }

    public List<BinaryFile> Files { get; set; } = [];
}
