using System.Xml.Linq;

namespace MetsParserTests
{
    public class MetsParserResolver
    {
        const string root1 = "C:\\git\\digirati-co-uk\\uol-leeds-experiments\\LeedsPrototypeTests\\samples\\10315s";

        [Fact]
        public void CanLoadFileLocation()
        {
            var parser = new MetsParser.Parser();

            var metsFile = parser.ResolveAndParseAsync(new Uri(root1));

            Assert.NotNull(metsFile);
        }

        [Fact]
        public void CanParseXml() 
        {
            var metsFilePath = root1 + "\\10315.METS.xml";
            var xMets = XDocument.Load(metsFilePath);
            var parser = new MetsParser.Parser();

            var metsFile = parser.Parse(new Uri(root1), xMets);

            Assert.NotNull(metsFile);

        }
    }
}