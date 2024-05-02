namespace Preservation.API.Data.Entities;

/// <summary>
/// Models a deposit in DB. "Entity" prefix used to easily differentiate this and API model (demo app, anything goes)
/// </summary>
/// <remarks>Initially based on https://github.com/uol-dlip/docs/blob/initial-sequence-diagrams/schema/deposits.sql.md</remarks>
public class DepositEntity
{
    public string Id { get; set; }
    public string PreservationPath { get; set; }
    public DateTime Created { get; set; }
    public string CreatedBy { get; set; }
    public DateTime? LastModified { get; set; }
    public string LastModifiedBy { get; set; }
    public string S3Root { get; set; }
    public string Status { get; set; }
    public string SubmissionText { get; set; }
    public DateTime? DatePreserved { get; set; }
    public DateTime? DateExported { get; set; }
    public string VersionExported { get; set; }
    public string VersionSaved { get; set; }
}