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


    public async Task<ArchivalGroup?> CreateArchivalGroup(string parentPath, string slug, string name, Transaction? transaction = null)
    {
        var req = MakeHttpRequestMessage(parentPath, HttpMethod.Post)
            .InTransaction(transaction)
            .WithName(name)
            .WithSlug(slug)
            .AsArchivalGroup();        
        var response = await _httpClient.SendAsync(req);
        response.EnsureSuccessStatusCode();

        // The body is the new resource URL
        var newReq = MakeHttpRequestMessage(response.Headers.Location!, HttpMethod.Get)
            .ForJsonLd();
        var newResponse = await _httpClient.SendAsync(newReq);

        var archivalGroupResponse = await MakeFedoraResponse<ArchivalGroupResponse>(newResponse);
        if(archivalGroupResponse != null)
        {
            var archivalGroup = new ArchivalGroup(archivalGroupResponse)
            {
                Identifier = archivalGroupResponse.Id,
                Directories = new List<Fedora.Directory>(),
                Files = new List<Fedora.File>()
            };

            // add the extra created/modified/by fields to Fedora.Resource
            return archivalGroup;
        }
        return null;
    }

    public async Task<Transaction> BeginTransaction()
    {
        var req = MakeHttpRequestMessage("fcr:tx", HttpMethod.Post);
        var response = await _httpClient.SendAsync(req);
        response.EnsureSuccessStatusCode();
        var tx = new Transaction
        {
            Location = response.Headers.Location!
        };
        if (response.Headers.TryGetValues("Atomic-Expires", out IEnumerable<string>? values))
        {
            tx.Expires = DateTime.Parse(values.First());
        }
        return tx;
    }

    public async Task CheckTransaction(Transaction tx)
    {
        HttpRequestMessage req = MakeHttpRequestMessage(tx.Location, HttpMethod.Get);
        var response = await _httpClient.SendAsync(req);
        switch (response.StatusCode)
        {
            case System.Net.HttpStatusCode.NoContent:
                tx.Expired = false;
                break;
            case System.Net.HttpStatusCode.NotFound:
                // error?
                break;
            case System.Net.HttpStatusCode.Gone:
                tx.Expired = true;
                break;
            default:
                // error?
                break;
        }
    }

    public async Task KeepTransactionAlive(Transaction tx)
    {
        HttpRequestMessage req = MakeHttpRequestMessage(tx.Location, HttpMethod.Post);
        var response = await _httpClient.SendAsync(req);
        response.EnsureSuccessStatusCode();

        if (response.Headers.TryGetValues("Atomic-Expires", out IEnumerable<string>? values))
        {
            tx.Expires = DateTime.Parse(values.First());
        }
    }

    public async Task CommitTransaction(Transaction tx)
    {
        HttpRequestMessage req = MakeHttpRequestMessage(tx.Location, HttpMethod.Put);
        var response = await _httpClient.SendAsync(req);
        switch (response.StatusCode)
        {
            case System.Net.HttpStatusCode.NoContent:
                tx.Committed = true;
                break;
            case System.Net.HttpStatusCode.NotFound:
                // error?
                break;
            case System.Net.HttpStatusCode.Conflict:
                tx.Committed = false;
                break;
            case System.Net.HttpStatusCode.Gone:
                tx.Expired = true;
                break;
            default:
                // error?
                break;
        }
        response.EnsureSuccessStatusCode();
    }

    public async Task RollbackTransaction(Transaction tx)
    {
        HttpRequestMessage req = MakeHttpRequestMessage(tx.Location, HttpMethod.Delete);
        var response = await _httpClient.SendAsync(req);
        switch (response.StatusCode)
        {
            case System.Net.HttpStatusCode.NoContent:
                tx.RolledBack = true;
                break;
            case System.Net.HttpStatusCode.NotFound:
                // error?
                break;
            case System.Net.HttpStatusCode.Gone:
                tx.Expired = true;
                break;
            default:
                // error?
                break;
        }
        response.EnsureSuccessStatusCode();
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
