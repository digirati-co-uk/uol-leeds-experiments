﻿namespace Fedora.Abstractions.Transfer;

/// <summary>
/// Used when importing new files into the repository from a source location (disk or S3)
/// or exporting to a location
/// </summary>
public class BinaryFile
{
    /// <summary>
    /// The repository path (not a full Uri), will end with Slug
    /// Only contains permitted characters (e.g., no spaces)
    /// </summary>
    public required string Path { get; set; }

    /// <summary>
    /// An S3 key, a filesystem path - somewhere accessible to the Preservation API, to import from or export to
    /// </summary>        
    public required string Location { get; set; }

    public required string StorageType { get; set; }

    /// <summary>
    /// Only contains permitted characters (e.g., no spaces)
    /// </summary>
    public string Slug => Path.Split('/')[^1];

    /// <summary>
    /// The name of the resource in Fedora (dc:title)
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    ///  The Original / actual name of the file, rather than the path-safe, reduced character set slug
    /// </summary>
    public required string FileName { get; set; }

    // NB ^^^ for a filename like readme.txt, Slug, Name and FileName will all be the same.
    // And in practice, Name and FileName are going ot be the same
    // But Slug may differ as it always must be in the reduced character set

    public required string ContentType { get; set; }

    public string? Digest { get; set; }
}