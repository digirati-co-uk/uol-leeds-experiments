using Fedora.Abstractions;

namespace Storage;

public interface IStorage
{
    // Getting things from Fedora
    // ==========================

    Task<Resource?> GetResource(string? path);
    string GetInternalPath(Uri preservationApiUri);
    Task<ArchivalGroup?> GetArchivalGroup(string path, string? version);
    Task<ResourceInfo> GetResourceInfo(string path);


    // Interacting with a staging area
    // ===============================

    /// <summary>
    /// In most instances the Storage API decides where to put this - in a bucket under a unique key - and then tells
    /// you where it is rather than you specifiying where you want it put. However, in some instances the Preservation
    /// API will know where it wants to place things. This means there's a dependency between Storage + Preservation API
    /// having write access to the same bucket. Original comment here had concerns of collision - this shouldn't be the
    /// case as Preservation-API will manage that.
    /// </summary>
    /// <param name="path">Repository path / identifier of Archival Group</param>
    /// <param name="version">(optional) The version to export; if omitted the HEAD (latest) is exported</param>
    /// <param name="destination">(optional) The version to export; if omitted the HEAD (latest) is exported</param>
    /// <returns></returns>
    Task<ExportResult> Export(string path, string? version, string? destination = null);
    
    /// <summary>
    /// An ImportJob is a representation of what needs doing to bring the repository ArchivalGroup to the same state
    /// as a set of files on disk or S3. It's a wrapper round a diff, that is then "executed" when it is sent to the Import endpoint.
    /// 
    /// The reason to split is to allow the operator (a human user, or software) to see the diff - to verify that the job is what
    /// was intended or expected.
    /// </summary>
    /// <param name="archivalGroupPath"></param>
    /// <param name="source"></param>
    /// <returns>A partially populated ImportJob</returns>
    Task<ImportJob> GetUpdateJob(string archivalGroupPath, string source);


    // Making changes to Fedora/OCFL objects (all CRUD)
    // ================================================

    /// <summary>
    /// "Execute" the update job obtained above.
    /// There is an `isUpdate` flag on ImportJob that must be explicitly set to true if a new version is intended.
    /// (to avoid unexpected overwrites).
    /// 
    /// The job will fail if, when entering a transaction, the ArchivalGroup is found to be at a later version than
    /// the one marked on the ImportJob.
    /// </summary>
    /// <param name="importJob"></param>
    /// <returns>A fully populated Job including the results</returns>    // 
    Task<ImportJob> Import(ImportJob importJob);

    // Organising the repository above the Archival Group level
    // ========================================================

    /// <summary>
    /// For creating containers outside of an archival group - or maybe even within one, later
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    Task<Container> CreateContainer(string path);
}
