using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fedora
{
    public abstract class Resource
    {
        // The original name of the resource (possibly non-filesystem-safe)
        // Use dc:title on the fedora resource
        public string? Name { get; set; }

        // The Fedora identifier
        public required string Identifier { get; set; }
    }
}
