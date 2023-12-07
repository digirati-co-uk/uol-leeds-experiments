using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fedora
{
    public interface IFedora
    {
        Task<string> Proxy(string contentType, string path, string? jsonLdMode = null, bool preferContained = false);
        Task<ArchivalGroup?> CreateArchivalGroup(string parentPath, string slug, string name);

    }
}
