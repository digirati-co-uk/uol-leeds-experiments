using Fedora.Abstractions.Transfer;

namespace MetsParser
{    
    public class MetsFile
    {
        // The title of the object the METS file describes
        public string? Name { get; set; }

        // The location of this METS file's parent directory; the METS file should be at its root
        public Uri? Root {  get; set; }

        // An entry describing the METS file itself, because it is not (typically) included in itself
        public BinaryFile? Self { get; set; }

        // A list of all the directories mentioned, with their names
        public List<ContainerDirectory> Directories { get; set; } = [];

        // A list of all the files mentioned, with their names and hashes (digests)
        public List<BinaryFile> Files { get; set; } = [];
    }
}
