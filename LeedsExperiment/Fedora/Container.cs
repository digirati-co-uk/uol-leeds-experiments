using Fedora.ApiModel;

namespace Fedora
{
    public class Container : Resource
    {
        public Container(FedoraJsonLdResponse jsonLdResponse) : base(jsonLdResponse)
        {
        }

        public required List<Container> Containers { get; set; } = new List<Container>();
        public required List<Binary> Binaries { get; set; } = new List<Binary>();
    }
}
