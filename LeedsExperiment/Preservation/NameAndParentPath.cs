namespace Storage;

public class NameAndParentPath
{
    public string Name { get; set; }
    public string? ParentPath { get; set; }

    public NameAndParentPath(string path)
    {
        var pathParts = path.Split(['/']);
        Name = pathParts[^1];
        if (pathParts.Length > 1)
        {
            ParentPath = path.Substring(0, path.Length - Name.Length - 1);
        }
    }
}
