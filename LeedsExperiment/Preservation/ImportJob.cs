using Fedora.Abstractions.Transfer;
using Fedora.Storage;

namespace Preservation;

public class ImportJob
{
    /// <summary>
    /// The Fedora path of the Archival Group
    /// </summary>
    public required string ArchivalGroupPath { get; set; }

    /// <summary>
    /// A filesystem or S3 path for the directory that will be compared to the archival object    /// 
    /// </summary>
    public required string Source { get; set; }

    /// <summary>
    /// Currently either FileSystem or S3
    /// </summary>
    public required string StorageType { get; set; }

    /// <summary>
    /// The Fedora Uri of the ArchivalGroup
    /// </summary>
    public Uri? ArchivalGroupUri { get; set; }

    /// <summary>
    /// When the diff calculation began
    /// </summary>
    public DateTime DiffStart { get; set; }

    /// <summary>
    /// When the diff calculation finished
    /// </summary>
    public DateTime DiffEnd { get; set; }
    
    /// <summary>
    /// What version at HEAD is this diff based on?
    /// When it comes to execute the job, need to make sure it's the same
    /// And then execute the update in a transaction.
    /// (null if a new object, IsUpdate = false)
    /// </summary>
    public ObjectVersion? DiffVersion { get; set; }

    /// <summary>
    /// Fedora containers that need to be created to synchronise the Archival Group object with the source
    /// </summary>
    public List<ContainerDirectory> ContainersToAdd { get; set; } = [];

    /// <summary>
    /// Fedora binaries that need to be created to synchronise the Archival Group object with the source
    /// </summary>
    public List<BinaryFile> FilesToAdd { get; set; } = [];

    /// <summary>
    /// Fedora binaries that need to be deleted to synchronise the Archival Group object with the source
    /// </summary>
    public List<BinaryFile> FilesToDelete { get; set; } = [];

    /// <summary>
    /// Fedora binaries that need to be UPDATED to synchronise the Archival Group object with the source
    /// Typically because their checksums don't match
    /// </summary>
    public List<BinaryFile> FilesToPatch { get; set; } = [];
    
    // FilesToRename? Can we even do that in Fedora?

    /// <summary>
    /// While any required new containers can be created as files are added (create along path),
    /// we may end up with containers that have no files in them; these need to be deleted from Fedora.
    /// </summary>
    public List<ContainerDirectory> ContainersToDelete { get; set; } = [];

    /// <summary>
    /// (populated when the job is executed)
    /// Any containers added to Fedora as part of this operation
    /// </summary>
    public List<ContainerDirectory> ContainersAdded { get; set; } = [];

    /// <summary>
    /// (populated when the job is executed)
    /// Any binaries added to Fedora as part of this operation
    /// </summary>
    public List<BinaryFile> FilesAdded { get; set; } = [];

    /// <summary>
    /// (populated when the job is executed)
    /// Any binaries deleted from Fedora as part of this operation
    /// (left as Tombstones)
    /// </summary>
    public List<BinaryFile> FilesDeleted { get; set; } = [];

    /// <summary>
    /// (populated when the job is executed)
    /// Any files UPDATED added in Fedora as part of this operation
    /// (typically, new binary content supplied, but could be other properties)
    /// </summary>
    public List<BinaryFile> FilesPatched { get; set; } = [];

    /// <summary>
    /// (populated when the job is executed)
    /// Any containers deleted from Fedora as part of this operation
    /// </summary>
    public List<ContainerDirectory> ContainersDeleted { get; set; } = [];

    /// <summary>
    /// Must be explicitly set to true to allow an update of an existing ArchivalGroup
    /// </summary>
    public bool IsUpdate { get; set; }

    /// <summary>
    /// When the job execution started
    /// </summary>
    public DateTime Start { get; set; }

    /// <summary>
    /// When the job execution finished
    /// </summary>
    public DateTime End { get; set; }

    /// <summary>
    /// (populated when the job is executed)
    /// The version that the Archival Group is now at
    /// Should be vNext OCFL, one higher than DiffVersion
    /// </summary>
    public ObjectVersion? NewVersion { get; set; }

}
