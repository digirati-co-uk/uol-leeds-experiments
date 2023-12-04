using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fedora
{
    public interface IFedora
    {
        public Task<string> Proxy(string contentType, string path, string? jsonLdMode = null, bool preferContained = false);

    }
}
