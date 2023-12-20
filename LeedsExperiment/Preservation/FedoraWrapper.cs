using Fedora;
using Fedora.ApiModel;
using Fedora.Vocab;
using System.Net;
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


    public async Task<ArchivalGroup?> CreateArchivalGroup(Uri parent, string slug, string name, Transaction? transaction = null)
    {
        return await CreateContainerInternal(true, parent, slug, name, transaction) as ArchivalGroup;
    }

    public async Task<ArchivalGroup?> CreateArchivalGroup(string parentPath, string slug, string name, Transaction? transaction = null)
    {
        var parent = new Uri(_httpClient.BaseAddress!, parentPath);
        return await CreateContainerInternal(true, parent, slug, name, transaction) as ArchivalGroup;
    }
    public async Task<Container?> CreateContainer(Uri parent, string slug, string name, Transaction? transaction = null)
    {
        return await CreateContainerInternal(false, parent, slug, name, transaction);
    }

    public async Task<Container?> CreateContainer(string parentPath, string slug, string name, Transaction? transaction = null)
    {
        var parent = new Uri(_httpClient.BaseAddress!, parentPath);
        return await CreateContainerInternal(false, parent, slug, name, transaction);
    }

    private async Task<Container?> CreateContainerInternal(bool isArchivalGroup, Uri parent, string slug, string name, Transaction? transaction = null)
    {
        var req = MakeHttpRequestMessage(parent, HttpMethod.Post)
            .InTransaction(transaction)
            .WithName(name)
            .WithSlug(slug);
        if (isArchivalGroup)
        {
            req.AsArchivalGroup();
        }
        var response = await _httpClient.SendAsync(req);
        response.EnsureSuccessStatusCode();

        // The body is the new resource URL
        var newReq = MakeHttpRequestMessage(response.Headers.Location!, HttpMethod.Get)
            .InTransaction(transaction)
            .ForJsonLd();
        var newResponse = await _httpClient.SendAsync(newReq);

        var containerResponse = await MakeFedoraResponse<FedoraJsonLdResponse>(newResponse);
        if (containerResponse != null)
        {
            if (isArchivalGroup)
            {
                return new ArchivalGroup(containerResponse)
                {
                    Location = containerResponse.Id,
                    Containers = [],
                    Binaries = []
                };
            } 
            else
            {
                return new Container(containerResponse)
                {
                    Location = containerResponse.Id,
                    Containers = [],
                    Binaries = []
                };

            }
        }
        return null;
    }

    // Deprecated, but leave the PutOrPost logic for reference
    public async Task<Binary> AddBinary(Uri parent, FileInfo localFile, string originalName, string contentType, Transaction? transaction = null, string? checksum = null)
    {
        return await PutOrPostBinary(HttpMethod.Post, parent, localFile, originalName, contentType, transaction, checksum);
    }

    public async Task<Binary> PutBinary(Uri location, FileInfo localFile, string originalName, string contentType, Transaction? transaction = null, string? checksum = null)
    {
        return await PutOrPostBinary(HttpMethod.Put, location, localFile, originalName, contentType, transaction, checksum);
    }

    private async Task<Binary> PutOrPostBinary(HttpMethod httpMethod, Uri location, FileInfo localFile, string originalName, string contentType, Transaction? transaction = null, string? checksum = null)
    {
        // verify that parent is a container first?
        var expected = Checksum.Sha512FromFile(localFile);
        if (checksum != null && checksum != expected)
        {
            throw new InvalidOperationException("Initial checksum doesn't match");
        }
        var req = MakeBinaryPutOrPost(httpMethod, location, localFile, originalName, contentType, transaction, expected);
        var response = await _httpClient.SendAsync(req);
        if (httpMethod == HttpMethod.Put && response.StatusCode == HttpStatusCode.Gone)
        {
            // https://github.com/fcrepo/fcrepo/pull/2044
            // see also https://github.com/whikloj/fcrepo4-tests/blob/fcrepo-6/archival_group_tests.py#L149-L190
            // 410 indicates that this URI has a tombstone sitting at it; it has previously been DELETEd.
            // But we want to reinstate a binary.

            // Log or record somehow that this has happened?
            var retryReq = MakeBinaryPutOrPost(httpMethod, location, localFile, originalName, contentType, transaction, expected)
                .OverwriteTombstone();
            response = await _httpClient.SendAsync(retryReq);
        }
        response.EnsureSuccessStatusCode();

        var resourceLocation = httpMethod == HttpMethod.Post ? response.Headers.Location! : location;
        var newReq = MakeHttpRequestMessage(resourceLocation.MetadataUri(), HttpMethod.Get)
            .InTransaction(transaction)
            .ForJsonLd();
        var newResponse = await _httpClient.SendAsync(newReq);

        var binaryResponse = await MakeFedoraResponse<BinaryMetadataResponse>(newResponse);
        if (binaryResponse.Title == null)
        {
            // The binary resource does not have a dc:title property yet
            var patchReq = MakeHttpRequestMessage(resourceLocation.MetadataUri(), HttpMethod.Patch)
                .InTransaction(transaction);
            patchReq.AsInsertTitlePatch(originalName);
            var patchResponse = await _httpClient.SendAsync(patchReq);
            patchResponse.EnsureSuccessStatusCode();
            // now ask again:
            var retryMetadataReq = MakeHttpRequestMessage(resourceLocation.MetadataUri(), HttpMethod.Get)
               .InTransaction(transaction)
               .ForJsonLd();
            var afterPatchResponse = await _httpClient.SendAsync(retryMetadataReq);
            binaryResponse = await MakeFedoraResponse<BinaryMetadataResponse>(afterPatchResponse);
        }
        var binary = new Binary(binaryResponse)
        {
            Location = binaryResponse.Id,
            FileName = binaryResponse.FileName,
            Size = binaryResponse.Size,
            Digest = binaryResponse.Digest?.Split(':')[^1],
            ContentType = binaryResponse.ContentType
        };
        if (binary.Digest != expected)
        {
            throw new InvalidOperationException("Fedora checksum doesn't match");
        }
        return binary;
    }

    private HttpRequestMessage MakeBinaryPutOrPost(HttpMethod httpMethod, Uri location, FileInfo localFile, string originalName, string contentType, Transaction? transaction, string? expected)
    {
        var req = MakeHttpRequestMessage(location, httpMethod)
            .InTransaction(transaction)
            .WithDigest(expected, "sha-512"); // move algorithm choice to config
        if (httpMethod == HttpMethod.Post)
        {
            req.WithSlug(localFile.Name);
        }

        // Need something better than this for large files.
        // How would we transfer a 10GB file for example?
        req.Content = new ByteArrayContent(File.ReadAllBytes(localFile.FullName))
            .WithContentType(contentType);

        // see if this survives the PUT (i.e., do we need to re-state it?)
        // No, always do this
        //if (httpMethod == HttpMethod.Post)
        //{ 
        req.Content.WithContentDisposition(originalName);
        //}
        return req;
    }

    public async Task<Transaction> BeginTransaction()
    {
        var req = MakeHttpRequestMessage("./fcr:tx", HttpMethod.Post); // note URI construction because of the colon
        var response = await _httpClient.SendAsync(req);
        response.EnsureSuccessStatusCode();
        var tx = new Transaction
        {
            Location = response.Headers.Location!
        };
        if (response.Headers.TryGetValues("Atomic-Expires", out IEnumerable<string>? values))
        {
            // This header is not being returned in the version we're using
            tx.Expires = DateTime.Parse(values.First());
        } 
        else
        {
            // ... so we'll need to obtain it like this, I think
            await KeepTransactionAlive(tx);
        }
        return tx;
    }

    public async Task CheckTransaction(Transaction tx)
    {
        HttpRequestMessage req = MakeHttpRequestMessage(tx.Location, HttpMethod.Get);
        var response = await _httpClient.SendAsync(req);
        switch (response.StatusCode)
        {
            case HttpStatusCode.NoContent:
                tx.Expired = false;
                break;
            case HttpStatusCode.NotFound:
                // error?
                break;
            case HttpStatusCode.Gone:
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
            case HttpStatusCode.NoContent:
                tx.Committed = true;
                break;
            case HttpStatusCode.NotFound:
                // error?
                break;
            case HttpStatusCode.Conflict:
                tx.Committed = false;
                break;
            case HttpStatusCode.Gone:
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
            case HttpStatusCode.NoContent:
                tx.RolledBack = true;
                break;
            case HttpStatusCode.NotFound:
                // error?
                break;
            case HttpStatusCode.Gone:
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
    public async Task<T?> GetObject<T>(string path, Transaction? transaction = null) where T : Resource
    {
        var uri = new Uri(_httpClient.BaseAddress!, path);
        return await GetObject<T>(uri, transaction);
    }

    public async Task<T?> GetObject<T>(Uri uri, Transaction? transaction = null) where T : Resource
    {
        var isBinary = typeof(T) == typeof(Binary);
        var reqUri = isBinary ? uri.MetadataUri() : uri;
        var request = MakeHttpRequestMessage(reqUri, HttpMethod.Get)
            .ForJsonLd(); 
        var response = await _httpClient.SendAsync(request);

        if (isBinary)
        {
            var fileResponse = await MakeFedoraResponse<BinaryMetadataResponse>(response);
            var binary = new Binary(fileResponse)
            {
                Location = fileResponse.Id,
                FileName = fileResponse.FileName,
                Size = fileResponse.Size,
                Digest = fileResponse.Digest,
                ContentType = fileResponse.ContentType
            };
            return binary as T;
        }
        else
        {
            var directoryResponse = await MakeFedoraResponse<FedoraJsonLdResponse>(response);
            if (directoryResponse != null)
            {
                if (response.HasArchivalGroupHeader())
                {
                    var ag = new ArchivalGroup(directoryResponse)
                    {
                        Location = directoryResponse.Id,
                        Containers = [],
                        Binaries = []
                    };
                    return ag as T;
                }
                else
                {
                    var container = new Container(directoryResponse)
                    {
                        Location = directoryResponse.Id,
                        Containers = [],
                        Binaries = []
                    };
                    return container as T;
                }
            }
        }
        return null;
    }

    public string? GetOrigin(ArchivalGroup versionedParent, Resource? childResource = null)
    {
        string basePath = _httpClient.BaseAddress.AbsolutePath;
        string absPath = versionedParent.Location?.AbsolutePath ?? string.Empty;
        if (!absPath.StartsWith(basePath))
        {
            return null;
        }        
        var idPart = absPath.Remove(0, basePath.Length);
        string parentOrigin = RepositoryPath.RelativeToRoot(idPart);

        return parentOrigin;

    }

    public async Task Delete(Uri uri, Transaction? transaction = null)
    {
        HttpRequestMessage req = MakeHttpRequestMessage(uri, HttpMethod.Delete)
            .InTransaction(transaction);

        var response = await _httpClient.SendAsync(req);
        response.EnsureSuccessStatusCode();
    }

}
