using Fedora;
using Preservation.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Preservation
{
    public class Fedora : IFedora
    {
        private readonly HttpClient _httpClient;

        public Fedora(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<string> Proxy(string contentType, string path, string? jsonLdMode = null, bool preferContained = false)
        {
            var req = new HttpRequestMessage();
            req.Headers.Accept.Clear();
            var contentTypeHeader = new MediaTypeWithQualityHeaderValue(contentType);
            if(contentType == ContentTypes.JsonLd)
            {
                if(jsonLdMode == JsonLdModes.Compacted)
                {
                    contentTypeHeader.Parameters.Add(new NameValueHeaderValue("profile", JsonLdModes.Compacted));
                }
                if (jsonLdMode == JsonLdModes.Flattened)
                {
                    contentTypeHeader.Parameters.Add(new NameValueHeaderValue("profile", JsonLdModes.Flattened));
                }
            }
            req.Headers.Accept.Add(contentTypeHeader);
            if (preferContained)
            {
                req.Headers.Add("Prefer", $"return=representation; include=\"{Prefer.PreferContainedDescriptions}\"");
            }
            req.RequestUri = new Uri(path, UriKind.Relative);
            var response = await _httpClient.SendAsync(req);
            var raw = await response.Content.ReadAsStringAsync();
            return raw;
        }
    }
}
