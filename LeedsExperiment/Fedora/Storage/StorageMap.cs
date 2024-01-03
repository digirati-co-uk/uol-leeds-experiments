namespace Fedora.Storage;

public class StorageMap
{
    // v1, v2
    public required ObjectVersion Version { get; set; }

    // e.g., S3
    public required string StorageType { get; set; }

    // e.g., a bucket
    public required string Root { get; set; }

    // The key of the object container
    public required string ObjectPath { get; set; }

    // What's the string key? - the relative file path.
    // The actual file path (FullPath in the value) may be versioned.
    public required Dictionary<string, OriginFile> Files { get; set; }

    public required ObjectVersion[] AllVersions { get; set; }
}

public class OriginFile
{
    public required string Hash { get; set; }
    public required string FullPath { get; set; }

    public override string ToString()
    {
        return FullPath;
    }
}
