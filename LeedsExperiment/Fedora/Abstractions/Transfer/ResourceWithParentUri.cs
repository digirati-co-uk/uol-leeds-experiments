namespace Fedora.Abstractions.Transfer;

public abstract class ResourceWithParentUri
{
    /// <summary>
    /// The repository path (not a full Uri), will end with Slug
    /// Only contains permitted characters (e.g., no spaces)
    /// 
    /// The Path is relative to some container or parent, which may not be the immediate parent.
    /// It will often be relative to the ArchivalGroup parent.
    /// </summary>
    public required string Path { get; set; }

    /// <summary>
    /// The Fedora repository Uri that is the parent of Path - i.e., where this file is to be put (or where it came from)
    /// </summary>
    public required Uri Parent { get; set; }

    /// <summary>
    /// The name of the resource in Fedora (dc:title)
    /// Usually the original name of the directory or file, which will usually be the same as Slug if all characters are path safe
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Only contains permitted characters (e.g., no spaces)
    /// </summary>
    public string Slug => Path.Split('/')[^1];

    public string GetDisplayName()
    {
        if(Name == Slug)
        {
            return Name;
        }
        return $"{Name} (/{Slug})";
    }
}
