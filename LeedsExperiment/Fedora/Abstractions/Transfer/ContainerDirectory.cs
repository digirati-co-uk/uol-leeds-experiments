namespace Fedora.Abstractions.Transfer;

public class ContainerDirectory
{
    /// <summary>
    /// The repository path (not a full Uri), will end with Slug
    /// Only contains permitted characters (e.g., no spaces)
    /// 
    /// This is not required if you supply Slug and a parent
    /// </summary>
    public string? Path { get; set; }

    private string? slug;
    public string? Slug
    {
        get
        {
            if (string.IsNullOrEmpty(Path))
            {
                return slug;
            }
            return Path.Split('/')[^1];
        }
        set
        {
            slug = value;
        }
    }

    /// <summary>
    /// The name of the resource in Fedora (dc:title)
    /// </summary>
    public required string Name { get; set; }
}
