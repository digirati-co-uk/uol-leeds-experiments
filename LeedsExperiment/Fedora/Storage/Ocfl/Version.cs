using System.Text.Json.Serialization;

namespace Fedora.Storage.Ocfl;

public class Version
{
    [JsonPropertyName("created")]
    [JsonPropertyOrder(1)]
    public DateTime Created { get; set; }

    [JsonPropertyName("user")]
    [JsonPropertyOrder(3)]
    public User User { get; set; }

    [JsonPropertyName("state")]
    [JsonPropertyOrder(4)]
    public Dictionary<string, string[]> State { get; set; }
}
