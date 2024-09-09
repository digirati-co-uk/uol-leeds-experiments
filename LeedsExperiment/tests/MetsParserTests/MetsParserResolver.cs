using System.Xml.Linq;

namespace MetsParserTests
{
    public class MetsParserResolver
    {

        [Fact]
        public async Task CanParseEPrintsMETS()
        {
            const string root1 = "file:///c:/git/digirati-co-uk/uol-leeds-experiments/LeedsPrototypeTests/samples/10315s";
            var parser = new MetsParser.Parser(null);

            var metsFile = await parser.ResolveAndParseAsync(new Uri(root1));

            Assert.NotNull(metsFile);

            Assert.NotNull(metsFile.Self);
            Assert.Equal("10315.METS.xml", metsFile.Self.Path);
            Assert.NotNull(metsFile.Self.Digest);

            Assert.Equal("[Example title]", metsFile.Name);
            Assert.Single(metsFile.Directories);
            Assert.Equal(4, metsFile.Files.Count);

            Assert.Equal("objects", metsFile.Directories[0].Name);
            Assert.Equal("4675c73e6fd66d2ea9a684ec79e4e6559bb4d44a35e8234794b0691472b0385d", metsFile.Files[0].Digest);
            Assert.Equal("315bb3bd2eb2da5ce8b848bb7f09803d8a48e64021c4b6fe074aed9cc591c154", metsFile.Files[3].Digest);

            Assert.Equal("objects/372705s_004.jpg", metsFile.Files[3].Path);
            Assert.Equal("372705s_004.jpg", metsFile.Files[3].Name);
            Assert.Equal("372705s_004.jpg", metsFile.Files[3].Slug);

            Assert.Equal(metsFile.Files[3].ExternalLocation, metsFile.Files[3].Parent + metsFile.Files[3].Path);
        }


        [Fact]
        public async Task CanParseWellcomeGoobiMETS()
        {
            const string root1 = "file:///c:/git/digirati-co-uk/uol-leeds-experiments/LeedsPrototypeTests/samples/wc-goobi";
            var parser = new MetsParser.Parser(null);

            var metsFile = await parser.ResolveAndParseAsync(new Uri(root1));

            Assert.NotNull(metsFile);

            Assert.NotNull(metsFile.Self);
            Assert.Equal("b29356350.xml", metsFile.Self.Path);
            Assert.NotNull(metsFile.Self.Digest);

            Assert.Equal("[Report 1960] /", metsFile.Name);
            Assert.Equal(2, metsFile.Directories.Count);
            Assert.Equal(64, metsFile.Files.Count);

            Assert.Equal("objects", metsFile.Directories[0].Name);
            Assert.Equal("alto", metsFile.Directories[1].Name);

            Assert.Equal(32, metsFile.Files.Count(f => f.Path.EndsWith(".jp2")));
            Assert.Equal(32, metsFile.Files.Count(f => f.Path.EndsWith(".xml")));

            Assert.Equal("objects/b29356350_0018.jp2", metsFile.Files[34].Path);
            Assert.Equal("b29356350_0018.jp2", metsFile.Files[34].Name);
            Assert.Equal("b29356350_0018.jp2", metsFile.Files[34].Slug);
            // Not a real sha-256 but it's fake in the METS file
            Assert.Equal("b16a6bb6281b273dc1f5035fe2a3699a288fd711", metsFile.Files[34].Digest);

            Assert.Equal("alto/b29356350_0018.xml", metsFile.Files[35].Path);
            Assert.Equal("b29356350_0018.xml", metsFile.Files[35].Name);
            Assert.Equal("b29356350_0018.xml", metsFile.Files[35].Slug);
            // There are no checksums for ALTOs in Goobi METS
            Assert.Null(metsFile.Files[35].Digest);
        }



        [Fact]
        public async Task CanParseArchivematicaMETS()
        {
            const string root1 = "file:///c:/git/digirati-co-uk/uol-leeds-experiments/LeedsPrototypeTests/samples/wc-archivematica";
            var parser = new MetsParser.Parser(null);

            var metsFile = await parser.ResolveAndParseAsync(new Uri(root1));

            Assert.NotNull(metsFile);

            Assert.NotNull(metsFile.Self);
            Assert.Equal("METS.299eb16f-1e62-4bf6-b259-c82146153711.xml", metsFile.Self.Path);
            Assert.NotNull(metsFile.Self.Digest);


            Assert.Null(metsFile.Name); // Not present in Archivematica METS
            Assert.Equal(11, metsFile.Directories.Count);
            Assert.Equal(38, metsFile.Files.Count);

            Assert.Contains(metsFile.Directories, d => d.Path == "objects/Edgware_Community_Hospital");
            Assert.Contains(metsFile.Files, f => f.Path == "objects/Edgware_Community_Hospital/03_05_01.tif");
            Assert.Contains(metsFile.Files, f => f.Path == "objects/Edgware_Community_Hospital/presentation_site_plan_A3.pdf");
            Assert.Contains(metsFile.Directories, d => d.Path == "objects/metadata");
            Assert.Contains(metsFile.Files, f => f.Path == "objects/metadata/transfers/ARTCOOB9-4840a241-d397-4554-abfe-69f1ad674126/rights.csv");

            // Now check for same name alterations
            // I edited the original METS to put spaces instead of underscores in the LABEL properties, so that 
            // they no longer match the file name on disk.
            var folder1 = metsFile.Directories.Single(d => d.Path == "objects/Edgware_Community_Hospital");
            Assert.Equal("Edgware Community Hospital", folder1.Name);

            var file1 = metsFile.Files.Single(f => f.Path == "objects/GJW_King_s_College_Hospital/Kings_1913_plan_altered.jpg");
            Assert.Equal("Kings 1913 plan altered.jpg", file1.Name);
        }

    }
}