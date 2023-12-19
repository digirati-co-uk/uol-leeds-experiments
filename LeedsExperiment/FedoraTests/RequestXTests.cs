using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FedoraTests
{
    public class RequestXTests
    {

        [Fact]
        public void MetadataUriAppendsCorrectly()
        {
            var baseUri = new Uri("https://fedora.org/path/to/a/binary");
            var metadataExt = new Uri("/fcr:metadata", UriKind.Relative);
            var metadataUri = new Uri(baseUri, metadataExt);

            Assert.Equal("https://fedora.org/path/to/a/binary/fcr:metadata", metadataUri.ToString());
        }

        [Fact]
        public void MetadataUriConcantenates()
        {
            var baseUri = new Uri("https://fedora.org/path/to/a/binary");
            var metadataUri = new Uri($"{baseUri}/fcr:metadata");

            Assert.Equal("https://fedora.org/path/to/a/binary/fcr:metadata", metadataUri.ToString());
        }
    }
}
