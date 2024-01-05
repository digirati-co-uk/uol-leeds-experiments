using Fedora.ApiModel;
using System.Text.Json.Serialization;

namespace Fedora
{
    public class Container : Resource
    {
        public Container(FedoraJsonLdResponse jsonLdResponse) : base(jsonLdResponse)
        {
            Type = "Container";
        }

        [JsonPropertyName("containers")]
        [JsonPropertyOrder(51)]
        public List<Container> Containers { get; set; } = new List<Container>();

        [JsonPropertyName("binaries")]
        [JsonPropertyOrder(52)]
        public List<Binary> Binaries { get; set; } = new List<Binary>();

        public override string ToString()
        {
            return $"📁 {Name ?? GetType().Name}";
        }
    }
}
