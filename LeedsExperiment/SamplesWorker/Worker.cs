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
            await fedora.CreateArchivalGroup("ag-root", Now(), "But same title");
        }

        private async Task OcflV1()
        {
            var path = @"C:\Users\TomCrane\Dropbox\digirati\leeds\fedora-experiments\versioned-example\working\v1";

            // begin transaction

            // make an archival group named Now()

            // POST the binary empty.text
            // POST the binary image.tiff
            // POST the basic container foo
            // POST into foo the binary bar.xml

            // end transaction

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
