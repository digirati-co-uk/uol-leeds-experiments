using Fedora;
using Preservation.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;

namespace Preservation
{
    public class Fedora : IFedora
    {
        private readonly HttpClient _httpClient;

        public Fedora(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<string> Proxy(string contentType, string path)
        {
            var req = new HttpRequestMessage();
            req.Headers.Accept.Clear();
            req.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue(contentType));
            req.RequestUri = new Uri(path, UriKind.Relative);
            var response = await _httpClient.SendAsync(req);
            var raw = await response.Content.ReadAsStringAsync();
            return raw;
        }
    }
}
