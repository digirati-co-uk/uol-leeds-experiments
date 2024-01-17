using Dashboard.Helpers;
using Fedora.Abstractions;

namespace Dashboard.Models;

public class ImportStartModel
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
}
