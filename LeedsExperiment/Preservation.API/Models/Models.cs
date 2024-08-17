using System.Text.Json.Serialization;

namespace Storage.API.Models;

/// <summary>
/// Base class for Preservation API models
/// </summary>
/// <remarks>
/// https://github.com/uol-dlip/docs/blob/rfc-001-what-store-in-fedora/rfcs/003-preservation-api.md#common-metadata
/// </remarks>
public abstract class PreservationResource
{
    [JsonPropertyName("@id")]
    [JsonPropertyOrder(1)]
    public Uri? Id { get; set; }
    
    [JsonPropertyOrder(2)]
    public abstract string Type { get; set; }
    
    [JsonPropertyOrder(3)]
    public DateTime? Created { get; set; }
    
    [JsonPropertyOrder(4)]
    public Uri? CreatedBy { get; set; }
    
    [JsonPropertyOrder(5)]
    public DateTime? LastModified { get; set; }
    
    [JsonPropertyOrder(6)]
    public Uri? LastModifiedBy { get; set; }
}

/// <summary>
/// A preserved digital object - e.g., the files that comprise a digitised book, or a manuscript, or a born digital item
/// A DigitalObject might only have one file, or may contain hundreds of files and directories (e.g., digitised images
/// and METS.xml). A DigitalObject is the unit of versioning - files within a DigitalObject cannot be separately
/// versioned, only the DigitalObject as a whole.
/// </summary>
/// <remarks>
/// https://github.com/uol-dlip/docs/blob/rfc-001-what-store-in-fedora/rfcs/003-preservation-api.md#-digitalobject
/// </remarks>
public class DigitalObject : PreservationResource
{
    [JsonPropertyOrder(2)]
    public override string Type { get; set; } = nameof(DigitalObject);
    
    /// <summary>
    /// Name of this <see cref="DigitalObject"/>
    /// </summary>
    [JsonPropertyOrder(9)]
    public string? Name { get; set; }
    
    /// <summary>
    /// Currently rendered version of object 
    /// </summary>
    [JsonPropertyOrder(10)]
    public DigitalObjectVersion? Version { get; set; }
    
    /// <summary>
    /// Previous versions of object 
    /// </summary>
    [JsonPropertyOrder(11)]
    public DigitalObjectVersion[]? Versions { get; set; }
    
    /// <summary>
    /// A list of the immediate child containers, if any.
    /// </summary>
    [JsonPropertyOrder(12)]
    public Container[]? Containers { get; set; }
    
    /// <summary>
    /// A list of the immediate contained binaries, if any.
    /// </summary>
    [JsonPropertyOrder(12)]
    public Binary[]? Binaries { get; set; }
}

public class DigitalObjectVersion
{
    /// <summary>
    /// Id directly to this version
    /// </summary>
    [JsonPropertyName("@id")]
    [JsonPropertyOrder(1)]
    public Uri? Id { get; set; }
    
    /// <summary>
    /// Name of this version (e.g. v1, v2 etc)
    /// </summary>
    [JsonPropertyOrder(2)]
    public string? Name { get; set; }
    
    /// <summary>
    /// Date of this version
    /// </summary>
    [JsonPropertyOrder(3)]
    public DateTime Date { get; set; }
}

/// <summary>
/// For building structure to organise the repository into a hierarchical layout
/// </summary>
/// <remarks>
/// https://github.com/uol-dlip/docs/blob/rfc-001-what-store-in-fedora/rfcs/003-preservation-api.md#-container
/// </remarks>
public class Container : PreservationResource
{
    public override string? Type { get; set; } = nameof(Container); 

    /// <summary>
    /// The original name, which may contain any UTF-8 character. Often this will be the same as the last path element
    /// of the @id, but it does not have to be.
    /// </summary>
    [JsonPropertyOrder(9)]
    public string? Name { get; set; }
    
    /// <summary>
    /// A list of the immediate child containers, if any.
    /// </summary>
    [JsonPropertyOrder(10)]
    public Container[]? Containers { get; set; }
    
    /// <summary>
    /// A list of the immediate contained binaries, if any.
    /// </summary>
    [JsonPropertyOrder(11)]
    public Binary[]? Binaries { get; set; }
    
    /// <summary>
    /// The @id of the DigitalObject the Container is in. Not present if the Container is outside a DigitalObject.
    /// </summary>
    [JsonPropertyOrder(12)]
    public Uri? PartOf { get; set; }
}

/// <summary>
/// For representing a file: any kind of file stored in the repository.
/// </summary>
/// <remarks>
/// https://github.com/uol-dlip/docs/blob/rfc-001-what-store-in-fedora/rfcs/003-preservation-api.md#-binary
/// </remarks>
public class Binary : PreservationResource
{
    public override string? Type { get; set; } = nameof(Binary);
    
    /// <summary>
    /// The original name, which may contain any UTF-8 character.
    /// </summary>
    [JsonPropertyOrder(9)]
    public string? Name { get; set; }
    
    /// <summary>
    /// The SHA-256 checksum for this file. This will always be returned by the API, but is only required when sending
    /// to the API if the checksum is not provided some other way
    /// </summary>
    [JsonPropertyOrder(10)]
    public string? Digest { get; set; }
    
    /// <summary>
    /// The S3 URI within a Deposit where this file may be accessed. If just browsing, this will be empty. If importing
    /// and sending this data to the API as part of an ImportJob, this is the S3 location the API should read the file
    /// from.
    /// </summary>
    [JsonPropertyOrder(11)]
    public Uri? Location { get; set; }
    
    /// <summary>
    /// An endpoint from which the binary content of the file may be retrieved (subject to authorisation). This is
    /// always provided by the API for API users to read a single file (it's not a location for the API to fetch from)
    /// </summary>
    [JsonPropertyOrder(12)]
    public Uri? Content { get; set; }
    
    /// <summary>
    /// The @id of the DigitalObject the Binary is in. Never null when returned by the API. Not required when sending as
    /// part of an ImportJob.
    /// </summary>
    [JsonPropertyOrder(13)]
    public Uri? PartOf { get; set; }
}

/// <summary>
/// A working set of files in S3, which will become a DigitalObject, or is used for updating a DigitalObject. API users
/// ask the Preservation API to create a Deposit, which returns an identifier and a working area in S3 (a key under
/// which to assemble files).
/// </summary>
/// <remarks>
/// https://github.com/uol-dlip/docs/blob/rfc-001-what-store-in-fedora/rfcs/003-preservation-api.md#deposit
/// </remarks>
public class Deposit : PreservationResource
{
    public override string Type { get; set; } = nameof(Deposit);
    
    /// <summary>
    /// The URI of the DigitalObject in the repository that this deposit will become (or was exported from).
    /// You don't need to provide this up front. You may not know it yet (e.g., you are appraising files). For some
    /// users, it will be assigned automatically. It may suit you to set this shortly before sending the deposit for
    /// preservation.
    /// </summary>
    [JsonPropertyOrder(10)]
    public Uri? DigitalObject { get; set; }
    
    /// <summary>
    /// An S3 key that represents a parent location. Use the "space" under this key to assemble files for an ImportJob.
    /// </summary>
    [JsonPropertyOrder(11)]
    public string Files { get; set; } 
    
    /// <summary>
    /// TBC - a step in a workflow.
    /// </summary>
    [JsonPropertyOrder(12)]
    public string Status { get; set; }
    
    /// <summary>
    /// A space to leave notes for colleagues or your future self
    /// </summary>
    [JsonPropertyOrder(13)]
    public string? SubmissionText { get; set; }
    
    /// <summary>
    /// Timestamp indicating when this deposit was last used to create an ImportJob for the Repository
    /// </summary>
    [JsonPropertyOrder(14)]
    public DateTime DatePreserved { get; set; }
    
    /// <summary>
    /// If this deposit was created as a result of asking the API to export a DigitalObject, the date that happened.
    /// </summary>
    [JsonPropertyOrder(15)]
    public DateTime? DateExported { get; set; }
    
    /// <summary>
    /// If this deposit was created as a result of asking the API to export a DigitalObject, the version of the Digital
    /// object that was exported then.
    /// </summary>
    [JsonPropertyOrder(16)]
    public DigitalObjectVersion? VersionExported { get; set; }
    
    /// <summary>
    /// If an Import Job is created from the files in this deposit and then sent to Preservation, the version that was
    /// created.
    /// </summary>
    [JsonPropertyOrder(17)]
    public DigitalObjectVersion? VersionSaved { get; set; }
    
    /// <summary>
    /// A list of jobs that have run on this deposit (TBC)
    /// </summary>
    [JsonPropertyOrder(18)]
    public string[] PipelineJobs { get; set; }
}

public class PreservationImportJob : PreservationResource
{
    public override string Type { get; set; } = "ImportJob";
    
    /// <summary>
    /// The Deposit that was used to generate this job, and to which it will be sent if executed.
    /// </summary>
    [JsonPropertyOrder(10)]
    public Uri Deposit { get; set; }
    
    /// <summary>
    /// The object in the repository that the job is to be performed on. This object doesn't necessarily exist yet -
    /// this job might be creating it. The value must match the digitalObject of the deposit, so it's technically
    /// redundant, but must be included so that the intent is explicit and self-contained.
    /// </summary>
    [JsonPropertyOrder(11)]
    public Uri DigitalObject { get; set; }
    
    /// <summary>
    /// Always provided when you ask the API to generate an ImportJob as a diff and the DigitalObject already exists.
    /// May be null for a new object
    /// </summary>
    [JsonPropertyOrder(12)]
    public DigitalObjectVersion? SourceVersion { get; set; }
    
    /// <summary>
    /// A list of Container objects to be created within the Digital object. The @id property gives the URI of the
    /// container to be created, whose path must be "within" the Digital Object and must only use characters from the
    /// permitted set. The name property of the container may be any UTF-8 characters, and can be used to preserve an
    /// original directory name.
    /// </summary>
    [JsonPropertyOrder(13)]
    public Container[] ContainersToAdd { get; set; }
    
    /// <summary>
    /// A list of Binary objects to be created within the Digital object from keys in S3. The @id property gives the URI
    /// of the binary to be created, whose path must be "within" the Digital Object and must only use characters from
    /// the permitted set. The name property of the Binary may be any UTF-8 characters, and can be used to preserve an
    /// original file name. The location must be an S3 key within the Deposit. The digest is only required if the SHA256
    /// cannot be obtained by the API from METS file information or from S3 metadata. All API-generated jobs will
    /// include this field. Note that in the second example above, the URI last path element, the name property, and the
    /// S3 location last path element are all different - this is permitted, although perhaps unusual.
    /// </summary>
    [JsonPropertyOrder(14)]
    public Binary[] BinariesToAdd { get; set; }
    
    /// <summary>
    /// A list of containers to remove. @id is the only required property. The Containers must either be already empty,
    /// or only contain Binaries mentioned in the binariesToDelete property of the same ImportJob.
    /// </summary>
    [JsonPropertyOrder(15)]
    public Container[] ContainersToDelete { get; set; }
    
    /// <summary>
    /// A list of binaries to remove. @id is the only required property.
    /// </summary>
    [JsonPropertyOrder(16)]
    public Binary[] BinariesToDelete { get; set; }
    
    /// <summary>
    /// A list of Binary objects to be updated within the Digital object from keys in S3. The @id property gives the URI
    /// of the binary to be patched, which must already exist. The name property of the Binary may be any UTF-8
    /// characters, and can be used to preserve an original file name. This may be different from the originally
    /// supplied name. The location must be an S3 key within the Deposit. The digest is only required if the SHA256
    /// cannot be obtained by the API from METS file information or from S3 metadata.
    /// </summary>
    [JsonPropertyOrder(17)]
    public Binary[] BinariesToPatch { get; set; }
}

public class ImportJobResult : PreservationResource
{
    public override string Type { get; set; } = nameof(ImportJobResult);
    
    /// <summary>
    /// A URI minted by the API which shows you the ImportJob submitted, for which this is the result. This is newly
    /// minted by the API when you actually submit an ImportJob, because: 1) not all Import Jobs are actually executed;
    /// 2) It may have been the special .../diff ImportJob; 3) It may have been an external identifier you provided.
    /// </summary>
    [JsonPropertyOrder(10)]
    public Uri ImportJob { get; set; }
    
    /// <summary>
    /// The @id property of the original submitted job
    /// </summary>
    [JsonPropertyOrder(11)]
    public Uri OriginalImportJobId { get; set; }
    
    /// <summary>
    /// Explicitly included for convenience; the deposit the job was started from.
    /// </summary>
    [JsonPropertyOrder(12)]
    public Uri Deposit { get; set; }
    
    /// <summary>
    /// Also included for convenience, the repository object the changes specified in the job are being applied to
    /// </summary>
    [JsonPropertyOrder(13)]
    public Uri DigitalObject { get; set; }

    /// <summary>
    /// One of "waiting", "running", "completed", "completedWithErrors"
    /// </summary>
    /// <remarks>Should this be an enum?</remarks>
    [JsonPropertyOrder(14)]
    public string Status { get; set; }
    
    /// <summary>
    /// Timestamp indicating when the API started processing the job. Will be null/missing until then.
    /// </summary>
    [JsonPropertyOrder(15)]
    public DateTime? DateBegun { get; set; }
    
    /// <summary>
    /// Timestamp indicating when the API finished processing the job. Will be null/missing until then.
    /// </summary>
    [JsonPropertyOrder(16)]
    public DateTime? DateFinished { get; set; }
    
    /// <summary>
    /// The version of the DigitalObject this job caused to be produced. Not known until the job has finished processing
    /// </summary>
    [JsonPropertyOrder(17)]
    public DigitalObjectVersion? NewVersion { get; set; }
    
    /// <summary>
    /// A list of errors encountered. These are error objects, not strings. 
    /// </summary>
    [JsonPropertyOrder(18)]
    public Error[]? Errors { get; set; }

    /// <summary>
    /// Populated once the job has finished successfully.
    /// </summary>
    [JsonPropertyOrder(19)]
    public Container[] ContainersAdded { get; set; } = Array.Empty<Container>();
    
    /// <summary>
    /// Populated once the job has finished successfully.
    /// </summary>
    [JsonPropertyOrder(20)]
    public Binary[] BinariesAdded { get; set; } = Array.Empty<Binary>();
    
    /// <summary>
    /// Populated once the job has finished successfully.
    /// </summary>
    [JsonPropertyOrder(21)]
    public Container[] ContainersDeleted { get; set; } = Array.Empty<Container>();
    
    /// <summary>
    /// Populated once the job has finished successfully.
    /// </summary>
    [JsonPropertyOrder(22)]
    public Binary[] BinariesDeleted { get; set; } = Array.Empty<Binary>();
    
    /// <summary>
    /// Populated once the job has finished successfully.
    /// </summary>
    [JsonPropertyOrder(23)]
    public Binary[] BinariesPatched { get; set; } = Array.Empty<Binary>();
}

/// <summary>
/// TBD - what fields does this class have?
/// </summary>
public class Error
{
    /// <summary>
    /// Id directly to this version
    /// </summary>
    [JsonPropertyName("@id")]
    [JsonPropertyOrder(1)]
    public Uri? Id { get; set; }
    
    /// <summary>
    /// 
    /// </summary>
    public string Message { get; set; }
}