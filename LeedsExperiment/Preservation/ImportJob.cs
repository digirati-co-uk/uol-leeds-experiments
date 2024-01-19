using Fedora.Abstractions.Transfer;
using Fedora.Storage;
using System.Text.Json.Serialization;

namespace Preservation;

public class ImportJob
{
    /// <summary>
    /// The Fedora path of the Archival Group
    /// </summary>
    [JsonPropertyName("archivalGroupPath")]
    [JsonPropertyOrder(1)]
    public required string ArchivalGroupPath { get; set; }

    /// <summary>
    /// A filesystem or S3 path for the directory that will be compared to the archival object    /// 
    /// </summary>
    [JsonPropertyName("source")]
    [JsonPropertyOrder(2)]
    public required string Source { get; set; }

    /// <summary>
    /// Currently either FileSystem or S3
    /// </summary>
    [JsonPropertyName("storageType")]
    [JsonPropertyOrder(3)]
    public required string StorageType { get; set; }

    /// <summary>
    /// The Fedora Uri of the ArchivalGroup
    /// </summary>
    [JsonPropertyName("archivalGroupUri")]
    [JsonPropertyOrder(4)]
    public Uri? ArchivalGroupUri { get; set; }

    /// <summary>
    /// The Name of the ArchivalGroup (dc:title), when creating a new one
    /// </summary>
    [JsonPropertyName("archivalGroupName")]
    [JsonPropertyOrder(5)]
    public string? ArchivalGroupName { get; set; }

    /// <summary>
    /// When the diff calculation began
    /// </summary>
    [JsonPropertyName("diffStart")]
    [JsonPropertyOrder(11)]
    public DateTime DiffStart { get; set; }

    /// <summary>
    /// When the diff calculation finished
    /// </summary>
    [JsonPropertyName("diffEnd")]
    [JsonPropertyOrder(12)]
    public DateTime DiffEnd { get; set; }

    /// <summary>
    /// What version at HEAD is this diff based on?
    /// When it comes to execute the job, need to make sure it's the same
    /// And then execute the update in a transaction.
    /// (null if a new object, IsUpdate = false)
    /// </summary>
    [JsonPropertyName("diffVersion")]
    [JsonPropertyOrder(14)]
    public ObjectVersion? DiffVersion { get; set; }

    /// <summary>
    /// Fedora containers that need to be created to synchronise the Archival Group object with the source
    /// </summary>
    [JsonPropertyName("containersToAdd")]
    [JsonPropertyOrder(21)]
    public List<ContainerDirectory> ContainersToAdd { get; set; } = [];

    /// <summary>
    /// Fedora binaries that need to be created to synchronise the Archival Group object with the source
    /// </summary>
    [JsonPropertyName("filesToAdd")]
    [JsonPropertyOrder(22)]
    public List<BinaryFile> FilesToAdd { get; set; } = [];

    /// <summary>
    /// Fedora binaries that need to be deleted to synchronise the Archival Group object with the source
    /// </summary>
    [JsonPropertyName("filesToDelete")]
    [JsonPropertyOrder(23)]
    public List<BinaryFile> FilesToDelete { get; set; } = [];

    /// <summary>
    /// Fedora binaries that need to be UPDATED to synchronise the Archival Group object with the source
    /// Typically because their checksums don't match
    /// </summary>
    [JsonPropertyName("filesToPatch")]
    [JsonPropertyOrder(24)]
    public List<BinaryFile> FilesToPatch { get; set; } = [];

    // FilesToRename? Can we even do that in Fedora?

    /// <summary>
    /// While any required new containers can be created as files are added (create along path),
    /// we may end up with containers that have no files in them; these need to be deleted from Fedora.
    /// </summary>
    [JsonPropertyName("containersToDelete")]
    [JsonPropertyOrder(25)]
    public List<ContainerDirectory> ContainersToDelete { get; set; } = [];

    /// <summary>
    /// (populated when the job is executed)
    /// Any containers added to Fedora as part of this operation
    /// </summary>
    [JsonPropertyName("containersAdded")]
    [JsonPropertyOrder(31)]
    public List<ContainerDirectory> ContainersAdded { get; set; } = [];

    /// <summary>
    /// (populated when the job is executed)
    /// Any binaries added to Fedora as part of this operation
    /// </summary>
    [JsonPropertyName("filesAdded")]
    [JsonPropertyOrder(32)]
    public List<BinaryFile> FilesAdded { get; set; } = [];

    /// <summary>
    /// (populated when the job is executed)
    /// Any binaries deleted from Fedora as part of this operation
    /// (left as Tombstones)
    /// </summary>
    [JsonPropertyName("filesDeleted")]
    [JsonPropertyOrder(33)]
    public List<BinaryFile> FilesDeleted { get; set; } = [];

    /// <summary>
    /// (populated when the job is executed)
    /// Any files UPDATED added in Fedora as part of this operation
    /// (typically, new binary content supplied, but could be other properties)
    /// </summary>
    [JsonPropertyName("filesPatched")]
    [JsonPropertyOrder(34)]
    public List<BinaryFile> FilesPatched { get; set; } = [];

    /// <summary>
    /// (populated when the job is executed)
    /// Any containers deleted from Fedora as part of this operation
    /// </summary>
    [JsonPropertyName("containersDeleted")]
    [JsonPropertyOrder(35)]
    public List<ContainerDirectory> ContainersDeleted { get; set; } = [];

    /// <summary>
    /// Must be explicitly set to true to allow an update of an existing ArchivalGroup
    /// </summary>
    [JsonPropertyName("isUpdate")]
    [JsonPropertyOrder(41)]
    public bool IsUpdate { get; set; }

    /// <summary>
    /// When the job execution started
    /// </summary>
    [JsonPropertyName("start")]
    [JsonPropertyOrder(51)]
    public DateTime Start { get; set; }

    /// <summary>
    /// When the job execution finished
    /// </summary>
    [JsonPropertyName("end")]
    [JsonPropertyOrder(52)]
    public DateTime End { get; set; }

    /// <summary>
    /// (populated when the job is executed)
    /// The version that the Archival Group is now at
    /// Should be vNext OCFL, one higher than DiffVersion
    /// </summary>
    [JsonPropertyName("newVersion")]
    [JsonPropertyOrder(61)]
    public ObjectVersion? NewVersion { get; set; }

}
