using Fedora.ApiModel;

namespace Fedora
{
    public class Binary : Resource
    {
        public Binary(FedoraJsonLdResponse jsonLdResponse) : base(jsonLdResponse)
        {
        }

        public string Origin 
        { 
            get
            {
                return "origin";
            } 
        }

        public string? FileName { get; set; }
        public string? ContentType { get; set; }
        public long Size { get; set; }
        public string? Digest { get; set; }

        public override string ToString()
        {
            return $"🗎 {Name ?? GetType().Name}";
        }
    }
}
