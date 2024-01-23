using Dashboard.Helpers;
using Fedora.Abstractions;
using Preservation;

namespace Dashboard.Models;

public class ImportModel
{
    public string DisplayName 
    { 
        get
        {
            if(ArchivalGroup == null)
            {
                return "[New Archival Group]";
            }
            return ArchivalGroup.GetDisplayName();
        } 
    }
    public required string Path { get; set; }
    public ArchivalGroup? ArchivalGroup { get; set; }
    public ResourceInfo? ResourceInfo { get; set; }
    public ImportJob? ImportJob { get; set; }

    /// <summary>
    ///  The name to give a new ArchivalGroup on creation
    /// </summary>
    public string? NewName { get; internal set; }
}
