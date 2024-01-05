using Fedora.ApiModel;
using System.Text.Json.Serialization;

namespace Fedora
{
    public class Binary : Resource
    {
        public Binary(FedoraJsonLdResponse jsonLdResponse) : base(jsonLdResponse)
        {
            Type = "Binary";
            var binaryresp = jsonLdResponse as BinaryMetadataResponse;
            if(binaryresp != null )
            {
                FileName = binaryresp.FileName;
                ContentType = binaryresp.ContentType;
                Size = Convert.ToInt64(binaryresp.Size);
                Digest = binaryresp.Digest?.Split(':')[^1];
            }
        }

        [JsonPropertyName("filename")]
        [JsonPropertyOrder(21)]
        public string? FileName { get; set; }

        [JsonPropertyName("contentType")]
        [JsonPropertyOrder(22)]
        public string? ContentType { get; set; }

        [JsonPropertyName("size")]
        [JsonPropertyOrder(23)]
        public long Size { get; set; }

        [JsonPropertyName("digest")]
        [JsonPropertyOrder(24)]
        public string? Digest { get; set; }

        public override string ToString()
        {
            return $"🗎 {Name ?? GetType().Name}";
        }
    }
}
