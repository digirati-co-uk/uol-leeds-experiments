using Fedora.ApiModel;
using System.Text.Json.Serialization;

namespace Fedora
{
    public class Container : Resource
    {
        public Container(FedoraJsonLdResponse jsonLdResponse) : base(jsonLdResponse)
        {
            if(jsonLdResponse.Type == null || jsonLdResponse.Type.Length == 0)
            {
                throw new InvalidOperationException("No type present");
            }
            if(jsonLdResponse.Type.Contains("fedora:RepositoryRoot"))
            {
                Type = "RepositoryRoot";
            }
            else if(jsonLdResponse.Type.Contains("http://purl.org/dc/dcmitype/Collection"))
            {
                // TODO - introduce this dcmi namespace and also check for dcmi:Collection (or whatever prefix)
                Type = "ArchivalGroup";
            }
            else
            {
                Type = "Container";
            }
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
