using Fedora;

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
                    var ag3 = await OcflV3(ag2);
                    break;
                default:
                    await DoDefault();
                    break;

            }
        }


        private async Task DoDefault()
        {
            // Console.WriteLine("supply something");
            await fedora.CreateArchivalGroup("ag-demo-root", Now(), "This is the title");
        }

        private async Task<ArchivalGroup> OcflV1()
        {
            var localPath = @"C:\Users\TomCrane\Dropbox\digirati\leeds\fedora-experiments\versioned-example\working\v1";

            // begin transaction
            var transaction = await fedora.BeginTransaction();
            Console.WriteLine("In transaction for v1 {0}", transaction.Location);

            var storageContainer = await fedora.GetObject<Container>("storage-01", transaction);
            
            var archivalGroup = await fedora.CreateArchivalGroup(storageContainer.Location, $"ocfl-expt-{Now()}", "ocflv1", transaction);

            // POST the binary image.tiff
            var localImageTiff = new FileInfo(Path.Combine(localPath, "image.tiff"));
            var fedoraImageTiff = await fedora.AddBinary(archivalGroup.Location, localImageTiff, localImageTiff.Name, "image/tiff", transaction);

            // POST the binary empty.text
            var localEmptyTxt = new FileInfo(Path.Combine(localPath, "empty.txt"));
            var fedoraEmptyTxt = await fedora.AddBinary(archivalGroup.Location, localEmptyTxt, localEmptyTxt.Name, "text/plain", transaction);


            // POST the basic container foo
            var fooDir = new DirectoryInfo(Path.Combine(localPath, "foo"));
            var fooContainer = await fedora.CreateContainer(archivalGroup.Location, fooDir.Name, fooDir.Name, transaction);

            // POST into foo the binary bar.xml
            var localBarXml = new FileInfo(Path.Combine(localPath, "foo", "bar.xml"));
            var fedoraBarXml = await fedora.AddBinary(fooContainer.Location, localBarXml, localBarXml.Name, "text/xml", transaction);

            await fedora.CommitTransaction(transaction);

            Console.WriteLine("archivalGroup at {0}", fedora.GetOrigin(archivalGroup));
            return archivalGroup;
        }

        private async Task<ArchivalGroup> OcflV2(ArchivalGroup archivalGroup)
        {
            var localPath = @"C:\Users\TomCrane\Dropbox\digirati\leeds\fedora-experiments\versioned-example\working\v2";

            var transaction = await fedora.BeginTransaction();
            Console.WriteLine("In transaction for v2 {0}", transaction.Location);

            // change content of foo/bar.xml
            var localBarXml = new FileInfo(Path.Combine(localPath, "foo", "bar.xml"));
            var putLocation = archivalGroup.GetResourceUri("foo/bar.xml");
            var fedoraBarXml = await fedora.PutBinary(putLocation, localBarXml, localBarXml.Name, "text/xml", transaction);

            // add empty2.txt
            var localEmpty2Txt = new FileInfo(Path.Combine(localPath, "empty2.txt"));
            var fedoraEmpty2Txt = await fedora.AddBinary(archivalGroup.Location, localEmpty2Txt, localEmpty2Txt.Name, "text/plain", transaction);

            // remove image.tiff
            await fedora.Delete(archivalGroup.GetResourceUri("image.tiff"), transaction);

            await fedora.CommitTransaction(transaction);

            var reObtainedAG = await fedora.GetObject<ArchivalGroup>(archivalGroup.Location);
            Console.WriteLine("archivalGroup v2 at {0}", fedora.GetOrigin(reObtainedAG));
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
            var localImageTiff = new FileInfo(Path.Combine(localPath, "image.tiff"));
            var fedoraImageTiff = await fedora.AddBinary(archivalGroup.Location, localImageTiff, localImageTiff.Name, "image/tiff", transaction);

            // end transaction
            await fedora.CommitTransaction(transaction);

            var reObtainedAG = await fedora.GetObject<ArchivalGroup>(archivalGroup.Location);
            Console.WriteLine("archivalGroup v3 at {0}", fedora.GetOrigin(reObtainedAG));
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
