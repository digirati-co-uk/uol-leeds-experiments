using System.Net.Http.Headers;
using Fedora.ApiModel;
using Fedora.Vocab;

namespace Preservation
{
    public static class RequestX
    {
        public static HttpRequestMessage ForJsonLd(this HttpRequestMessage requestMessage)
        {
            requestMessage.Headers.Accept.Clear();
            var contentTypeHeader = new MediaTypeWithQualityHeaderValue("application/ld+json");
            contentTypeHeader.Parameters.Add(new NameValueHeaderValue("profile", JsonLdModes.Compacted));
            requestMessage.Headers.Accept.Add(contentTypeHeader);
            return requestMessage;
        }

        public static HttpRequestMessage WithContainedDescriptions(this HttpRequestMessage requestMessage)
        {
            requestMessage.Headers.Add("Prefer", $"return=representation; include=\"{Prefer.PreferContainedDescriptions}\"");
            return requestMessage;
        }

        public static HttpRequestMessage InTransaction(this HttpRequestMessage requestMessage, Transaction? transaction)
        {
            if(transaction != null)
            {
                requestMessage.Headers.Add(Transaction.HeaderName, transaction.Location.ToString());
            }
            return requestMessage;
        }

        public static HttpRequestMessage WithName(this HttpRequestMessage requestMessage, string? name)
        {
            if(requestMessage.Content == null && !string.IsNullOrWhiteSpace(name)) 
            {
                var turtle = MediaTypeHeaderValue.Parse("text/turtle");
                requestMessage.Content = new StringContent($"PREFIX dc: <http://purl.org/dc/elements/1.1/>  <> dc:title \"{name}\"", turtle);
            }
            return requestMessage;
        }

        public static HttpRequestMessage WithContentDisposition(this HttpRequestMessage requestMessage, string? contentDisposition)
        {
            if (!string.IsNullOrWhiteSpace(contentDisposition))
            {
                requestMessage.Headers.Add("Content-Disposition", $"attachment; filename=\"{contentDisposition}\""); 
            }
            return requestMessage;
        }

        public static HttpRequestMessage WithDigest(this HttpRequestMessage requestMessage, string? digest)
        {
            if (!string.IsNullOrWhiteSpace(digest))
            {
                requestMessage.Headers.Add("digest", $"sha-256={digest}");
            }
            return requestMessage;
        }

        public static HttpRequestMessage WithSlug(this HttpRequestMessage requestMessage, string slug) 
        {
            requestMessage.Headers.Add("slug", slug);
            return requestMessage;
        }

        public static HttpRequestMessage AsArchivalGroup(this HttpRequestMessage requestMessage)
        {
            requestMessage.Headers.Add("Link", $"<{RepositoryTypes.ArchivalGroup}>;rel=\"type\"");
            return requestMessage;
        }

        /// <summary>
        /// This should really obtain the rel=describedBy link via a HEAD request
        /// But in the interests of efficienct, we'll be a little less RESTful and
        /// assume a Fedora convention.
        /// </summary>
        /// <param name="resourceUri"></param>
        /// <returns></returns>
        public static Uri MetadataUri(this Uri resourceUri)
        {
            return new Uri(resourceUri, "fcr:metadata");
        }
    }
}
