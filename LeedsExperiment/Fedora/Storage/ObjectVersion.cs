namespace Fedora.Storage;

public class ObjectVersion
{
    public required string MementoTimestamp { get; set; }
    public required DateTime MementoDateTime { get; set; }
    public string? OcflVersion { get; set; }

    public override string ToString()
    {
        if(OcflVersion == null)
        {
            return MementoTimestamp;
        }
        return $"{OcflVersion} | {MementoTimestamp}";
    }

}
