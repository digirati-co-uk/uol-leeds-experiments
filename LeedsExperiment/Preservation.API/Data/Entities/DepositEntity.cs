namespace Preservation.API.Data.Entities;

/// <summary>
/// Models a deposit in DB. "Entity" prefix used to easily differentiate this and API model (demo app, anything goes)
/// </summary>
/// <remarks>Initially based on https://github.com/uol-dlip/docs/blob/initial-sequence-diagrams/schema/deposits.sql.md</remarks>
public class DepositEntity
{
    /// <summary>
    /// Unique Id of this deposit - corresponds with folder in S3 
    /// </summary>
    public required string Id { get; set; }
    
    /// <summary>
    /// The URI of the DigitalObject in the repository that this deposit will become
    /// </summary>
    public Uri? PreservationPath { get; set; }
    
    public DateTime Created { get; set; }
    public string CreatedBy { get; set; }
    public DateTime? LastModified { get; set; }
    public string? LastModifiedBy { get; set; }
    
    /// <summary>
    /// Root folder where this deposit will be arranged (files added etc)
    /// </summary>
    public Uri S3Root { get; set; }
    
    public string Status { get; set; } = "new";
    public string? SubmissionText { get; set; }
    public DateTime? DatePreserved { get; set; }
    public DateTime? DateExported { get; set; }
    public string? VersionExported { get; set; }
    public string? VersionSaved { get; set; }
}