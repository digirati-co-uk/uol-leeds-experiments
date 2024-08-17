using Fedora.Storage;
using System.Text.Json.Serialization;

namespace Storage;

public class ExportResult
{
    /// <summary>
    /// The Fedora path of the Archival Group
    /// </summary>
    [JsonPropertyName("archivalGroupPath")]
    [JsonPropertyOrder(1)]
    public required string ArchivalGroupPath { get; set; }

    /// <summary>
    /// The S3 location (later maybe other locations) to which the object was exported
    /// </summary>
    [JsonPropertyName("destination")]
    [JsonPropertyOrder(2)]
    public required string Destination { get; set; }

    /// <summary>
    /// Currently either FileSystem or S3
    /// </summary>
    [JsonPropertyName("storageType")]
    [JsonPropertyOrder(3)]
    public required string StorageType { get; set; }

    /// <summary>
    /// The version that was exported
    /// </summary>
    [JsonPropertyName("version")]
    [JsonPropertyOrder(4)]
    public required ObjectVersion Version { get; set; }

    /// <summary>
    /// When the export started
    /// </summary>
    [JsonPropertyName("start")]
    [JsonPropertyOrder(11)]
    public DateTime Start { get; set; }

    /// <summary>
    /// When the export finished
    /// </summary>
    [JsonPropertyName("end")]
    [JsonPropertyOrder(12)]
    public DateTime End { get; set; }

    /// <summary>
    /// A list of all the files exported - S3 URIs, typically; could be filesystem paths later
    /// </summary>
    [JsonPropertyName("files")]
    [JsonPropertyOrder(21)]
    public List<string> Files { get; set; } = [];

    [JsonPropertyName("problem")]
    [JsonPropertyOrder(31)]
    public string? Problem {  get; set; }
}
