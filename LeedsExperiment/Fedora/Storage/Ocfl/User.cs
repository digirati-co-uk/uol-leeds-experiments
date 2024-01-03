using System.Text.Json.Serialization;

namespace Fedora.Storage.Ocfl;

public class User
{
    [JsonPropertyName("name")]
    [JsonPropertyOrder(1)]
    public DateTime Name { get; set; }


    [JsonPropertyName("address")]
    [JsonPropertyOrder(2)]
    public DateTime Address { get; set; }
}
