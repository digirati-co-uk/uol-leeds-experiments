namespace Fedora.Storage;

public class ObjectVersion
{
    public string MementoTimestamp { get; set; }
    public DateTime MementoDateTime { get; set; }
    public string OcflVersion { get; set; }

    public override string ToString()
    {
        return MementoTimestamp;
    }

}
