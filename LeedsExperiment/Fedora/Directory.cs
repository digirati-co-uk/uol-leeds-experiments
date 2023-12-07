using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fedora
{
    public class Directory : Resource
    {
        public required List<Directory> Directories { get; set; } = new List<Directory>();
        public required List<File> Files { get; set; } = new List<File>();
    }
}
