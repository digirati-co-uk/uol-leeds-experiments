namespace Fedora.Abstractions.Transfer;

public class ContainerDirectory
{
    /// <summary>
    /// The repository path (not a full Uri), will end with Slug
    /// Only contains permitted characters (e.g., no spaces)
    /// </summary>
    public required string Path { get; set; }

    public string Slug => Path.Split('/')[^1];

    /// <summary>
    /// The name of the resource in Fedora (dc:title)
    /// </summary>
    public required string Name { get; set; }
}
