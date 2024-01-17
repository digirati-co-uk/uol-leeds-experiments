namespace Preservation;

public class PreservationApiOptions
{
    public required string Prefix { get; set; }
    public required string StagingBucket { get; set; }
    public required int StorageMapCacheTimeSeconds { get; set; } = 5;
}
