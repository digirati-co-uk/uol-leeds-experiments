using Fedora.Abstractions.Transfer;
using Fedora.Storage;

namespace Preservation;

public class ImportJob
{
    public required string ArchivalGroupPath { get; set; }
    // Must be an S3 URI, for now
    public required string Source { get; set; }
    public required string StorageType { get; set; }

    public Uri? ArchivalGroupUri { get; set; }


    public DateTime DiffStart { get; set; }
    public DateTime DiffEnd { get; set; }
    
    /// <summary>
    /// What version at HEAD is this diff based on?
    /// When it comes to execute the job, need to make sure it's the same
    /// And then execute the update in a transaction.
    /// (null if a new object, IsUpdate = false)
    /// </summary>
    public ObjectVersion? DiffVersion { get; set; }

    public List<ContainerDirectory> ContainersToAdd { get; set; } = [];
    public List<BinaryFile> FilesToAdd { get; set; } = [];
    public List<BinaryFile> FilesToDelete { get; set; } = [];
    public List<BinaryFile> FilesToPatch { get; set; } = [];
    // FilesToRename?

    /// <summary>
    /// While any required new containers can be created as files are added (create along path),
    /// we may end up with containers that have no files in them; these need to be deleted from Fedora.
    /// </summary>
    public List<ContainerDirectory> ContainersToDelete { get; set; } = [];


    public List<ContainerDirectory> ContainersAdded { get; set; } = [];
    public List<BinaryFile> FilesAdded { get; set; } = [];
    public List<BinaryFile> FilesDeleted { get; set; } = [];
    public List<BinaryFile> FilesPatched { get; set; } = [];
    public List<ContainerDirectory> ContainersDeleted { get; set; } = [];

    // Must be explicitly set to true to allow an update of an existing ArchivalGroup
    public bool IsUpdate { get; set; }


    public DateTime Start { get; set; }
    public DateTime End { get; set; }


    public ObjectVersion? NewVersion { get; set; }


}
