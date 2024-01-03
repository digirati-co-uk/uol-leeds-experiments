using System.Text.Json.Serialization;

namespace Fedora.Storage.Ocfl;

public class User
{
    [JsonPropertyName("name")]
    [JsonPropertyOrder(1)]
    public string Name { get; set; }


    [JsonPropertyName("address")]
    [JsonPropertyOrder(2)]
    public string Address { get; set; }
}
