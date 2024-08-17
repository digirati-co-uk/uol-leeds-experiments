using Dashboard.Helpers;
using Fedora.Abstractions;
using Storage;

namespace Dashboard.Models;

public class ImportModel
{
    public string DisplayName => ArchivalGroup?.GetDisplayName() ?? Name!;
    public required string Path { get; set; }
    public ArchivalGroup? ArchivalGroup { get; set; }
    public ResourceInfo? ResourceInfo { get; set; }
    public ImportJob? ImportJob { get; set; }

    /// <summary>
    ///  The name to give a new Archival Group on creation, or the name of the current
    ///  Archival Group.
    /// </summary>
    public string? Name { get; internal set; }
}
