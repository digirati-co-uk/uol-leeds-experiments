using System.Text.Json.Serialization;

namespace Fedora.Storage.Ocfl;

public class Inventory
{
    [JsonPropertyName("id")]
    [JsonPropertyOrder(1)]
    public string Id { get; set; }

    [JsonPropertyName("digestAlgorithm")]
    [JsonPropertyOrder(3)]
    public string DigestAlgorithm { get; set; }

    [JsonPropertyName("head")]
    [JsonPropertyOrder(4)]
    public string Head { get; set; }

    [JsonPropertyName("contentDirectory")]
    [JsonPropertyOrder(5)]
    public string ContentDirectory { get; set; }

    [JsonPropertyName("manifest")]
    [JsonPropertyOrder(6)]
    public Dictionary<string, string[]> Manifest { get; set; }

    [JsonPropertyName("versions")]
    [JsonPropertyOrder(7)]
    public Dictionary<string, Version> Versions { get; set; }


}
