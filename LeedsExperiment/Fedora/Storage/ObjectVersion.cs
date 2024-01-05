using System.Text.Json.Serialization;

namespace Fedora.Storage;

public class ObjectVersion
{
    [JsonPropertyName("mementoTimestamp")]
    [JsonPropertyOrder(1)]
    public required string MementoTimestamp { get; set; }

    [JsonPropertyName("mementoDateTime")]
    [JsonPropertyOrder(2)]
    public required DateTime MementoDateTime { get; set; }

    [JsonPropertyName("ocflVersion")]
    [JsonPropertyOrder(3)]
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
