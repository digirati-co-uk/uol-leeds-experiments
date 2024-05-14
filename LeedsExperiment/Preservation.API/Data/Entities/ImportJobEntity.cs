namespace Preservation.API.Data.Entities;

public class ImportJobEntity
{
    public required string Id { get; set; }
    
    public required Uri OriginalImportJobId { get; set; }
    
    public string Deposit { get; set; }
    
    public required Uri DigitalObject { get; set; }
    
    public string Status { get; set; } = ImportJobStates.Waiting;
    
    /// <summary>
    /// When the job was submitted to API
    /// </summary>
    public DateTime? DateSubmitted { get; set; }
    
    /// <summary>
    /// When the API started processing the job
    /// </summary>
    public DateTime? DateBegun { get; set; }
    
    /// <summary>
    /// When the API finished processing the job
    /// </summary>
    public DateTime? DateFinished { get; set; }
    
    /// <summary>
    /// The version of the DigitalObject this job caused to be produced
    /// </summary>
    public string? NewVersion { get; set; }
    
    public string? Errors { get; set; }
    public string? ContainersAdded { get; set; }
    public string? BinariesAdded { get; set; }
    public string? ContainersDeleted { get; set; }
    public string? BinariesDeleted { get; set; }
    public string? BinariesPatched { get; set; }
}

public static class ImportJobStates
{
    public const string Waiting = "waiting";
    public const string Running = "running";
    public const string Completed = "completed";
    public const string CompletedWithErrors = "completedWithErrors";
}