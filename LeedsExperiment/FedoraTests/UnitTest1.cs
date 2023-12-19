using System.IO;
using System.Net;

namespace FedoraTests
{
    public class UnitTest1
    {
        [Fact]
        public void RelativeUriTest()
        {
            var relativeUri = new Uri("fcr:tx", UriKind.Relative);
            var baseUri = new Uri("https://fedora.org/rest/");
            var combinedUri = new Uri(baseUri, relativeUri);

            Assert.Equal("https://fedora.org/rest/fcr:tx", combinedUri.ToString());

        }


        [Fact]
        public void RelativeUriTest3()
        {
            var relativeUri = new Uri("./fcr:tx", UriKind.Relative);
            var baseUri = new Uri("https://fedora.org/rest/");
            var combinedUri = new Uri(baseUri, relativeUri);

            Assert.Equal("https://fedora.org/rest/fcr:tx", combinedUri.ToString());

        }


        [Fact]
        public void RelativeUriTest4()
        {
            var relativeUri = new Uri("./fcr:tx", UriKind.Relative);
            var baseUri = new Uri("https://fedora.org/rest/image.tiff/");
            var combinedUri = new Uri(baseUri, relativeUri);

            Assert.Equal("https://fedora.org/rest/image.tiff/fcr:tx", combinedUri.ToString());

        }


        [Fact]
        public void RelativeUriTest5()
        {
            var relativeUri = new Uri("./fcr:tx", UriKind.Relative);
            var baseUri = new Uri("https://fedora.org/rest/imagetiff/");
            var combinedUri = new Uri(baseUri, relativeUri);

            Assert.Equal("https://fedora.org/rest/imagetiff/fcr:tx", combinedUri.ToString());

        }

        [Fact]
        public void RelativeUriTest2()
        {
            var relativeUri = new Uri("rest/fcr:tx", UriKind.Relative);
            var baseUri = new Uri("https://fedora.org/");
            var combinedUri = new Uri(baseUri, relativeUri);

            Assert.Equal("https://fedora.org/rest/fcr:tx", combinedUri.ToString());

        }


        [Fact]
        public void RelativeEscapedUriTest()
        {
            var escaped = WebUtility.UrlEncode("fcr:tx");
            var relativeUri = new Uri(escaped, UriKind.Relative);
            var baseUri = new Uri("https://fedora.org/rest/");
            var combinedUri = new Uri(baseUri, relativeUri);

            Assert.Equal("https://fedora.org/rest/fcr:tx", combinedUri.ToString());

        }


        [Fact]
        public void AbsoluteUriTest()
        {
            var uri = new Uri("https://fedora.org/rest/fcr:tx", UriKind.Absolute);
            Assert.Equal("https://fedora.org/rest/fcr:tx", uri.ToString());

        }

    }
}