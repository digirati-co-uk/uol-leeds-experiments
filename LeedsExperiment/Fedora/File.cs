using Fedora.ApiModel;

namespace Fedora
{
    public class File : Resource
    {
        public File(FedoraJsonLdResponse jsonLdResponse) : base(jsonLdResponse)
        {
        }

        public string Origin 
        { 
            get
            {
                return "origin";
            } 
        }

        // checksums, fixity

        // roles? probably not

        // technical metadata? maybe not

        // content type?
    }
}
