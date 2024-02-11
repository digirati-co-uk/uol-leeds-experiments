using Dlcs.Hydra;
using Fedora.Abstractions;

namespace Dashboard.Models;

public class IIIFSyncModel
{
    public required string Path { get; set; }

    public Dictionary<string, Image?> ImageMap { get; } = [];
    public ArchivalGroup? ArchivalGroup { get; set; }
    public Batch? Batch { get; set; }
}
