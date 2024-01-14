using Fedora.Abstractions;

namespace Preservation;

public interface IPreservation
{
    // Getting things from Fedora
    Task<Resource?> GetResource(string? path);
    string GetInternalPath(Uri preservationApiUri);
    Task<ArchivalGroup?> GetArchivalGroup(string path, string? version);


    // Interacting with a staging area
    Task<ExportResult> Export(string path, string? version);
    
    /// <summary>
    /// Get a diff that can then be executed 
    /// </summary>
    /// <param name="path"></param>
    /// <param name="source"></param>
    /// <returns></returns>
    Task<ImportJob> GetUpdateJob(string path, string source);

    // Create or update the job obtained above - latter requires isUpdate explicitly
    Task<ImportJob> Import(ImportJob importJob);
}
