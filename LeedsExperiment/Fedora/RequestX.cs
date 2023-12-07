using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;
using Fedora.Vocab;

namespace Fedora
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

        public static HttpRequestMessage WithName(this HttpRequestMessage requestMessage, string? name)
        {
            if(requestMessage.Content == null && !string.IsNullOrWhiteSpace(name)) 
            {
                requestMessage.Content = new StringContent($"PREFIX dc: <http://purl.org/dc/elements/1.1/>  <> dc:title \"{name}\"");
            }
            return requestMessage;
        }

        public static HttpRequestMessage WithSlug(this HttpRequestMessage requestMessage, string slug) 
        {
            requestMessage.Headers.Add("slug", slug);
            return requestMessage;
        }
    }
}
