using Fedora;
using Fedora.ApiModel;
using Fedora.Vocab;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace Preservation;

public class FedoraWrapper : IFedora
{
    private readonly HttpClient _httpClient;

    public FedoraWrapper(HttpClient httpClient)
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
            req.WithContainedDescriptions();
        }
        req.RequestUri = new Uri(path, UriKind.Relative);
        var response = await _httpClient.SendAsync(req);
        var raw = await response.Content.ReadAsStringAsync();
        return raw;
    }


    public async Task<ArchivalGroup?> CreateArchivalGroup(string parentPath, string slug, string name)
    {
        var uri = new Uri(parentPath, UriKind.Relative);
        var req = MakeHttpRequestMessage(parentPath, HttpMethod.Post);
        // .ForJsonLd() // no? for POST?

        // Now uncomment these:

        // .WithName(name)
        // .WithSlug(slug);
        // req.Headers.Add("Link", $"<{RepositoryTypes.ArchivalGroup}>;rel=\"type\"");
        var response = await _httpClient.SendAsync(req);
        response.EnsureSuccessStatusCode();

        // The body is the new resource URL
        var newReq = MakeHttpRequestMessage(response.Headers.Location!, HttpMethod.Get).ForJsonLd();
        var newResponse = await _httpClient.SendAsync(newReq);

        var archivalGroupResponse = await MakeFedoraResponse<ArchivalGroupResponse>(newResponse);
        var ag = new ArchivalGroup
        {
            Name = archivalGroupResponse.Title,
            Identifier = archivalGroupResponse.Id,
            Directories = new List<Fedora.Directory>(),
            Files = new List<Fedora.File>()
        };

        // add the extra created/modified/by fields to Fedora.Resource
        return ag;
    }

    private HttpRequestMessage MakeHttpRequestMessage(string path, HttpMethod method)
    {
        var uri = new Uri(path, UriKind.Relative);
        return MakeHttpRequestMessage(uri, method);
    }

    private HttpRequestMessage MakeHttpRequestMessage(Uri uri, HttpMethod method)
    {
        var requestMessage = new HttpRequestMessage(method, uri);
        requestMessage.Headers.Accept.Clear();
        return requestMessage;
    }

    private static async Task<T?> MakeFedoraResponse<T>(HttpResponseMessage response) where T : FedoraJsonLdResponse
    {
        var fedoraResponse = await response.Content.ReadFromJsonAsync<T>();
        if (fedoraResponse != null)
        {
            fedoraResponse.HttpResponseHeaders = response.Headers;
            fedoraResponse.HttpStatusCode = response.StatusCode;
            fedoraResponse.Body = await response.Content.ReadAsStringAsync();
        }
        return fedoraResponse;
    }
}
