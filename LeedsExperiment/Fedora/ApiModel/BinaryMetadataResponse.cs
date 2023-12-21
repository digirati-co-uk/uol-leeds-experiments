using System.Text.Json.Serialization;

namespace Fedora.ApiModel
{
    public class BinaryMetadataResponse : FedoraJsonLdResponse
    {
        [JsonPropertyName("filename")]
        [JsonPropertyOrder(201)]
        public string? FileName { get; set; }

        [JsonPropertyName("hasMimeType")]
        [JsonPropertyOrder(202)]
        public string? ContentType { get; set; }

        // This comes back as a string
        [JsonPropertyName("hasSize")]
        [JsonPropertyOrder(203)]
        public string Size { get; set; }

        [JsonPropertyName("hasMessageDigest")]
        [JsonPropertyOrder(211)]
        public string? Digest { get; set; }        
    }
}
