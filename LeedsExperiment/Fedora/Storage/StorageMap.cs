namespace Fedora.Storage;

public class StorageMap
{
    // v1, v2
    public string Version {  get; set; }

    // e.g., S3
    public string StorageType { get; set; }

    // e.g., a bucket
    public string Root { get; set; }

    // The key of the object container
    public string ObjectPath { get; set; }

    public Dictionary<string, OriginFile> Files { get; set; }
}

public class OriginFile
{
    public string Hash { get; set; }
    public string FullPath { get; set; }
}
