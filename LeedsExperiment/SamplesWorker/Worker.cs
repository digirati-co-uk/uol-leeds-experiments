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
            switch (args?[0]?.ToLowerInvariant())
            {
                case "ocfl":
                    await OcflV1();
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

        private async Task OcflV1()
        {
            var localPath = @"C:\Users\TomCrane\Dropbox\digirati\leeds\fedora-experiments\versioned-example\working\v1";

            // begin transaction
            var transaction = await fedora.BeginTransaction();
            var storageContainer = await fedora.GetObject<Container>("storage-01", transaction);
            
            await fedora.CreateArchivalGroup(storageContainer.Location, $"ocfl-expt-{Now()}", "ocflv1", transaction);

            // POST the binary empty.text
            var localEmptyTxt = new FileInfo(Path.Combine(localPath, "empty.txt"));
            var fedoraEmptyTxt = await fedora.AddBinary(storageContainer.Location, localEmptyTxt, localEmptyTxt.Name, transaction);

            // POST the binary image.tiff
            var localImageTiff = new FileInfo(Path.Combine(localPath, "image.tiff"));
            var fedoraImageTiff = await fedora.AddBinary(storageContainer.Location, localImageTiff, localImageTiff.Name, transaction);

            // POST the basic container foo
            var fooDir = new DirectoryInfo(Path.Combine(localPath, "foo"));
            var fooContainer = await fedora.CreateContainer(storageContainer.Location, fooDir.Name, fooDir.Name, transaction);

            // POST into foo the binary bar.xml
            var localBarXml = new FileInfo(Path.Combine(localPath, "foo", "bar.xml"));
            var fedoraBarXml = await fedora.AddBinary(fooContainer.Location, localBarXml, localBarXml.Name, transaction);

            await fedora.CommitTransaction(transaction);
        }

        private async Task OcflV2()
        {
            // begin transaction

            // change content of foo/bar.xml
            // add empty2.txt
            // remove image.tiff

            // end transaction

        }

        private async Task OcflV3()
        {
            // begin transaction

            // remove empty.txt
            // restore image.tiff

            // end transaction

        }

        /*
         * Given a path in S3, and an intended Fedora path, create a new Fedora archival group object.
         * 
         * Given a path in S3 and a fedora archival group object, see if there is any difference between the fedora object and the content under the path.
         * 
         * Given a diff, update the fedora object to match the layout under the S3 path and stamp a new version.
         * 
         * Request that a set of files are placed on S3 corresponding to an existing fedora archival object.
         * 
         * 
         */

        private static string Now()
        {
            return DateTime.Now.ToString("MM-dd-yy-H-mm-ss");
        }
    }
}
