
using Fedora.Abstractions.Transfer;
using Fedora.Storage;

namespace Preservation;

public class ExportResult
{
    /// <summary>
    /// The Fedora path of the Archival Group
    /// </summary>
    public required string ArchivalGroupPath { get; set; }


    /// <summary>
    /// The S3 location (later maybe other locations) to which the object was exported
    /// </summary>
    public required string Destination { get; set; }

    /// <summary>
    /// Currently either FileSystem or S3
    /// </summary>
    public required string StorageType { get; set; }

    /// <summary>
    /// The version that was exported
    /// </summary>
    public required ObjectVersion Version { get; set; }

    /// <summary>
    /// When the export started
    /// </summary>
    public DateTime Start { get; set; }

    /// <summary>
    /// When the export finished
    /// </summary>
    public DateTime End { get; set; }

    /// <summary>
    /// A list of all the files exported
    /// </summary>
    public List<BinaryFile> Files { get; set; } = [];
}
