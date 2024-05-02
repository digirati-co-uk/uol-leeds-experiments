namespace Preservation.API;

public class PreservationSettings
{
    public required Uri StorageApiBaseAddress { get; set; }
    
    /// <summary>
    /// Bucket to use for storing deposits
    /// </summary>
    public required string DepositBucket { get; set; }
    
    /// <summary>
    /// Prefix to add to all deposits in bucket (must end with trailing '/') 
    /// </summary>
    public required string DepositKeyPrefix { get; set; }
}