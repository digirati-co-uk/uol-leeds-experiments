using Fedora.ApiModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fedora
{
    public class ArchivalGroup : Directory
    {
        public ArchivalGroup(FedoraJsonLdResponse fedoraResponse) : base(fedoraResponse)
        {

        }
    }
}
