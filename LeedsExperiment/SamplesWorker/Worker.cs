using Fedora;
using Fedora.Abstractions;
using Fedora.Abstractions.Transfer;
using Fedora.Storage;
using Storage;

namespace SamplesWorker
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> logger;
        private IFedora fedora;

        public Worker(ILogger<Worker> logger, IFedora fedora)
        {
            this.logger = logger;
            this.fedora = fedora;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await EntryPoint(Environment.GetCommandLineArgs(), stoppingToken);

        }

        private async Task EntryPoint(string[] args, CancellationToken stoppingToken)
        {
            switch (args?[1]?.ToLowerInvariant())
            {
                case "ocfl":
                    var ag = await OcflV1();
                    var ag2 = await OcflV2(ag);
                    // Can't run 3 until tombstone issue resolved
                    var ag3 = await OcflV3(ag2);
                    // And now it is!

                    //var altAg3 = await OcflV3Alt(ag2);
                    //var altAg4 = await OcflV4Alt(altAg3);
                    break;

                case "ag":
                    var path = "storage-01/ocfl-expt-01-03-24-10-06-50";
                    await PrintArchivalGroup(path);
                    break;

                default:
                    await DoDefault();
                    break;

            }
        }

        private async Task PrintArchivalGroup(string path)
        {
            var headAg = await fedora.GetPopulatedArchivalGroup(path);
            Console.WriteLine("This is the HEAD archival object for {0}", path);
            printArchivalGroup(headAg);
            foreach (var version in headAg.Versions)
            {
                Console.WriteLine();
                Console.WriteLine("--------------------------------------------------------");
                Console.WriteLine("This is version {0} for archival object {1}", version.OcflVersion, path);
                var vAg = await fedora.GetPopulatedArchivalGroup(path, version.OcflVersion);
                printArchivalGroup(vAg);
            }
            foreach (var version in headAg.Versions)
            {
                Console.WriteLine();
                Console.WriteLine("--------------------------------------------------------");
                Console.WriteLine("This is version {0} for archival object {1}", version.MementoTimestamp, path);
                var vAg = await fedora.GetPopulatedArchivalGroup(path, version.MementoTimestamp);
                printArchivalGroup(vAg);
            }
        }

        private void printArchivalGroup(ArchivalGroup vAg)
        {
            Console.WriteLine(vAg.Version);
        }

        private async Task DoDefault()
        {
            // Console.WriteLine("supply something");
            await fedora.CreateArchivalGroup("ag-demo-root", Now(), "This is the title");
        }

        /// <summary>
        /// This is just a helper method for this test to use the local file system.
        /// Our path (slug), filename (content-disposition) and name (the dc:title) are all the same, just the file name.
        /// </summary>
        /// <param name="localRootPath"></param>
        /// <param name="pathWithinArchivalGroup"></param>
        /// <param name="contentType"></param>
        /// <returns></returns>
        private BinaryFile MakeBinaryFileFromFileSystem(string localRootPath, Uri fedoraParent, string pathWithinArchivalGroup, string contentType)
        {
            // This still seems a bit awkward to construct - paths
            var localFileInfo = new FileInfo(Path.Combine(localRootPath, pathWithinArchivalGroup.Replace('/', Path.DirectorySeparatorChar)));
            var sha256 = Checksum.Sha256FromFile(localFileInfo);
            return new BinaryFile
            {
                Parent = fedoraParent,
                ExternalLocation = localFileInfo.FullName,
                Path = pathWithinArchivalGroup,
                ContentType = contentType,
                // FileName = localFileInfo.Name,
                Name = localFileInfo.Name,
                StorageType = StorageTypes.FileSystem,
                Digest = sha256
            };
        }

        private async Task<ArchivalGroup> OcflV1()
        {
            var localPath = @"C:\Users\TomCrane\Dropbox\digirati\leeds\fedora-experiments\versioned-example\working\v1";

            // begin transaction
            var transaction = await fedora.BeginTransaction();
            Console.WriteLine("In transaction for v1 {0}", transaction.Location);

            var storageContainer = await fedora.GetObject<Container>("storage-01", transaction);

            var name = $"ocfl-expt-{Now()}";
            var archivalGroup = await fedora.CreateArchivalGroup(storageContainer.Location, name, name, transaction);

            // PUT the binary image.tiff
            var binaryFileTiff = MakeBinaryFileFromFileSystem(localPath, archivalGroup.Location, "image.tiff", "image/tiff");            
            var fedoraImageTiff = await fedora.PutBinary(binaryFileTiff, transaction);

            // POST the binary empty.text
            var binaryEmptyTxt = MakeBinaryFileFromFileSystem(localPath, archivalGroup.Location, "empty.txt", "text/plain");
            var fedoraEmptyTxt = await fedora.PutBinary(binaryEmptyTxt, transaction);

            // POST the basic container foo
            var fooDir = new DirectoryInfo(Path.Combine(localPath, "foo"));
            var cd = new ContainerDirectory { Parent=archivalGroup.Location, Name = fooDir.Name, Path = fooDir.Name };
            var fooContainer = await fedora.CreateContainer(cd, transaction);

            // POST into foo the binary bar.xml
            var binaryBarXml = MakeBinaryFileFromFileSystem(localPath, archivalGroup.Location, "foo/bar.xml", "text/xml");
            var fedoraBarXml = await fedora.PutBinary(binaryBarXml, transaction);

            await fedora.CommitTransaction(transaction);

            var reObtainedAG = await fedora.GetPopulatedArchivalGroup(archivalGroup.Location);
            Console.WriteLine("archivalGroup at {0}", reObtainedAG.Origin);
            return archivalGroup;
        }

        private async Task<ArchivalGroup> OcflV2(ArchivalGroup archivalGroup)
        {
            var localPath = @"C:\Users\TomCrane\Dropbox\digirati\leeds\fedora-experiments\versioned-example\working\v2";

            var transaction = await fedora.BeginTransaction();
            Console.WriteLine("In transaction for v2 {0}", transaction.Location);

            // change content of foo/bar.xml
            var binaryBarXml = MakeBinaryFileFromFileSystem(localPath, archivalGroup.Location, "foo/bar.xml", "text/xml");
            var fedoraBarXml = await fedora.PutBinary(binaryBarXml, transaction);

            // add empty2.txt
            var binaryEmptyTxt = MakeBinaryFileFromFileSystem(localPath, archivalGroup.Location, "empty2.txt", "text/plain");
            var fedoraEmptyTxt = await fedora.PutBinary(binaryEmptyTxt, transaction);

            // remove image.tiff
            await fedora.Delete(archivalGroup.GetResourceUri("image.tiff"), transaction);

            await fedora.CommitTransaction(transaction);

            var reObtainedAG = await fedora.GetPopulatedArchivalGroup(archivalGroup.Location);
            Console.WriteLine("archivalGroup v2 at {0}", reObtainedAG.Origin);
            return reObtainedAG;
        }

        private async Task<ArchivalGroup> OcflV3(ArchivalGroup archivalGroup)
        {
            var localPath = @"C:\Users\TomCrane\Dropbox\digirati\leeds\fedora-experiments\versioned-example\working\v3";

            var transaction = await fedora.BeginTransaction();
            Console.WriteLine("In transaction for v3 {0}", transaction.Location);

            // remove empty.txt
            await fedora.Delete(archivalGroup.GetResourceUri("empty.txt"), transaction);

            // restore image.tiff
            // so should this be a POST or a PUT?
            var binaryFileTiff = MakeBinaryFileFromFileSystem(localPath, archivalGroup.Location, "image.tiff", "image/tiff");
            var fedoraImageTiff = await fedora.PutBinary(binaryFileTiff, transaction);

            // end transaction
            await fedora.CommitTransaction(transaction);

            var reObtainedAG = await fedora.GetPopulatedArchivalGroup(archivalGroup.Location);
            Console.WriteLine("archivalGroup v3 at {0}", reObtainedAG.Origin);
            return reObtainedAG;
        }


        private async Task<ArchivalGroup> OcflV3Alt(ArchivalGroup archivalGroup)
        {
            var localPath = @"C:\Users\TomCrane\Dropbox\digirati\leeds\fedora-experiments\versioned-example\working\v3Alt";

            var transaction = await fedora.BeginTransaction();
            Console.WriteLine("In transaction for v3Alt {0}", transaction.Location);

            // Add another image
            var binaryFileJpg = MakeBinaryFileFromFileSystem(localPath, archivalGroup.Location, "picture.jpg", "image/jpeg");
            var fedoraImageJpg = await fedora.PutBinary(binaryFileJpg, transaction);

            // end transaction
            await fedora.CommitTransaction(transaction);

            var reObtainedAG = await fedora.GetPopulatedArchivalGroup(archivalGroup.Location);
            Console.WriteLine("archivalGroup v3Alt at {0}", reObtainedAG.Origin);
            return reObtainedAG;
        }

        private async Task<ArchivalGroup> OcflV4Alt(ArchivalGroup archivalGroup)
        {
            var localPath = @"C:\Users\TomCrane\Dropbox\digirati\leeds\fedora-experiments\versioned-example\working\v4Alt";

            var transaction = await fedora.BeginTransaction();
            Console.WriteLine("In transaction for v4Alt {0}", transaction.Location);

            // CHANGE the image at the picture.jpg URI
            var binaryFileJpg = MakeBinaryFileFromFileSystem(localPath, archivalGroup.Location, "picture.jpg", "image/jpeg");
            var fedoraImageJpg = await fedora.PutBinary(binaryFileJpg, transaction);

            // end transaction
            await fedora.CommitTransaction(transaction);

            var reObtainedAG = await fedora.GetPopulatedArchivalGroup(archivalGroup.Location);
            Console.WriteLine("archivalGroup v4Alt at {0}", reObtainedAG.Origin);
            return reObtainedAG;
        }


        /*
         * Demonstrate that we can 
         *  - enumerate the files mentioned above
         *  - print out their S3 origins
         *  
         *  call a dump method? populate the files and folders recursively?
         *  
         *  Then do the next bit:         
         * 
         * 
         * Given a path in S3, and an intended Fedora path, create a new Fedora archival group object.
         * 
         * Given a path in S3 and a fedora archival group object, see if there is any difference between the fedora object and the content under the path.
         * 
         * Given a diff, update the fedora object to match the layout under the S3 path and stamp a new version.
         * 
         * Request that a set of files are placed on S3 corresponding to an existing fedora archival object.
         * 
         * The same, but a particular version
         * 
         * Simulate a METS file - a document that has relative paths to other files
         * 
         * Generate a storage map... as a first class concept of our wrapper
         * 
         */

        private static string Now()
        {
            return DateTime.Now.ToString("MM-dd-yy-H-mm-ss");
        }
    }
}
