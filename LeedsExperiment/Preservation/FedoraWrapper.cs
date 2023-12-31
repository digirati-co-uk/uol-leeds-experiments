﻿using Fedora;
using Fedora.ApiModel;
using Fedora.Storage;
using Fedora.Vocab;
using System.IO;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace Preservation;

public class FedoraWrapper : IFedora
{
    private readonly HttpClient httpClient;
    private readonly IStorageMapper storageMapper;

    public FedoraWrapper(HttpClient httpClient, IStorageMapper storageMapper)
    {
        this.httpClient = httpClient;
        this.storageMapper = storageMapper;
    }

    public async Task<string> Proxy(string contentType, string path, string? jsonLdMode = null, bool preferContained = false, string? acceptDate = null, bool head = false)
    {
        var req = new HttpRequestMessage();
        req.Headers.Accept.Clear();
        req.RequestUri = new Uri(path, UriKind.Relative);
        req.WithAcceptDate(acceptDate);

        if (head)
        {
            req.Method = HttpMethod.Head;
            var headResponse = await httpClient.SendAsync(req);
            var sb = new StringBuilder("<table border=1 cellpadding=3>");
            foreach(var h in headResponse.Headers)
            {
                sb.AppendLine($"<tr><td valign=top>{h.Key}</td><td valign=top>");
                foreach (var v in h.Value)
                {
                    sb.AppendLine($"<code>{WebUtility.HtmlEncode(v)}</code><br/>");
                }
                sb.AppendLine("</td></tr>");
            }
            sb.AppendLine("</table>");
            return sb.ToString();
        }

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
        var response = await httpClient.SendAsync(req);
        var raw = await response.Content.ReadAsStringAsync();
        return raw;
    }


    public async Task<ArchivalGroup?> CreateArchivalGroup(Uri parent, string slug, string name, Transaction? transaction = null)
    {
        return await CreateContainerInternal(true, parent, slug, name, transaction) as ArchivalGroup;
    }

    public async Task<ArchivalGroup?> CreateArchivalGroup(string parentPath, string slug, string name, Transaction? transaction = null)
    {
        var parent = new Uri(httpClient.BaseAddress!, parentPath);
        return await CreateContainerInternal(true, parent, slug, name, transaction) as ArchivalGroup;
    }
    public async Task<Container?> CreateContainer(Uri parent, string slug, string name, Transaction? transaction = null)
    {
        return await CreateContainerInternal(false, parent, slug, name, transaction);
    }

    public async Task<Container?> CreateContainer(string parentPath, string slug, string name, Transaction? transaction = null)
    {
        var parent = new Uri(httpClient.BaseAddress!, parentPath);
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
        var response = await httpClient.SendAsync(req);
        response.EnsureSuccessStatusCode();

        // The body is the new resource URL
        var newReq = MakeHttpRequestMessage(response.Headers.Location!, HttpMethod.Get)
            .InTransaction(transaction)
            .ForJsonLd();
        var newResponse = await httpClient.SendAsync(newReq);

        var containerResponse = await MakeFedoraResponse<FedoraJsonLdResponse>(newResponse);
        if (containerResponse != null)
        {
            if (isArchivalGroup)
            {
                return new ArchivalGroup(containerResponse) { Location = containerResponse.Id };
            } 
            else
            {
                return new Container(containerResponse) { Location = containerResponse.Id };
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
        var response = await httpClient.SendAsync(req);
        if (httpMethod == HttpMethod.Put && response.StatusCode == HttpStatusCode.Gone)
        {
            // https://github.com/fcrepo/fcrepo/pull/2044
            // see also https://github.com/whikloj/fcrepo4-tests/blob/fcrepo-6/archival_group_tests.py#L149-L190
            // 410 indicates that this URI has a tombstone sitting at it; it has previously been DELETEd.
            // But we want to reinstate a binary.

            // Log or record somehow that this has happened?
            var retryReq = MakeBinaryPutOrPost(httpMethod, location, localFile, originalName, contentType, transaction, expected)
                .OverwriteTombstone();
            response = await httpClient.SendAsync(retryReq);
        }
        response.EnsureSuccessStatusCode();

        var resourceLocation = httpMethod == HttpMethod.Post ? response.Headers.Location! : location;
        var newReq = MakeHttpRequestMessage(resourceLocation.MetadataUri(), HttpMethod.Get)
            .InTransaction(transaction)
            .ForJsonLd();
        var newResponse = await httpClient.SendAsync(newReq);

        var binaryResponse = await MakeFedoraResponse<BinaryMetadataResponse>(newResponse);
        if (binaryResponse.Title == null)
        {
            // The binary resource does not have a dc:title property yet
            var patchReq = MakeHttpRequestMessage(resourceLocation.MetadataUri(), HttpMethod.Patch)
                .InTransaction(transaction);
            patchReq.AsInsertTitlePatch(originalName);
            var patchResponse = await httpClient.SendAsync(patchReq);
            patchResponse.EnsureSuccessStatusCode();
            // now ask again:
            var retryMetadataReq = MakeHttpRequestMessage(resourceLocation.MetadataUri(), HttpMethod.Get)
               .InTransaction(transaction)
               .ForJsonLd();
            var afterPatchResponse = await httpClient.SendAsync(retryMetadataReq);
            binaryResponse = await MakeFedoraResponse<BinaryMetadataResponse>(afterPatchResponse);
        }
        var binary = new Binary(binaryResponse)
        {
            Location = binaryResponse.Id,
            FileName = binaryResponse.FileName,
            Size = Convert.ToInt64(binaryResponse.Size),
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
        var response = await httpClient.SendAsync(req);
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
        var response = await httpClient.SendAsync(req);
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
        var response = await httpClient.SendAsync(req);
        response.EnsureSuccessStatusCode();

        if (response.Headers.TryGetValues("Atomic-Expires", out IEnumerable<string>? values))
        {
            tx.Expires = DateTime.Parse(values.First());
        }
    }

    public async Task CommitTransaction(Transaction tx)
    {
        HttpRequestMessage req = MakeHttpRequestMessage(tx.Location, HttpMethod.Put);
        var response = await httpClient.SendAsync(req);
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
        var response = await httpClient.SendAsync(req);
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
        // works for SINGLE resources, not contained responses that send back a @graph
        var fedoraResponse = await response.Content.ReadFromJsonAsync<T>();
        if (fedoraResponse != null)
        {
            fedoraResponse.HttpResponseHeaders = response.Headers;
            fedoraResponse.HttpStatusCode = response.StatusCode;
            fedoraResponse.Body = await response.Content.ReadAsStringAsync();
        }
        return fedoraResponse;
    }

    public async Task<Resource?> GetObject(string path, Transaction? transaction = null)
    {
        var uri = new Uri(httpClient.BaseAddress!, path);
        return await GetObject(uri, transaction);
    }

    public async Task<T?> GetObject<T>(string path, Transaction? transaction = null) where T : Resource
    {
        var uri = new Uri(httpClient.BaseAddress!, path);
        return await GetObject<T>(uri, transaction);
    }

    public async Task<Resource?> GetObject(Uri uri, Transaction? transaction = null)
    {
        // Make a head request to see what this is
        var req = new HttpRequestMessage(HttpMethod.Head, uri);
        req.Headers.Accept.Clear();
        var headResponse = await httpClient.SendAsync(req);
        if (headResponse.HasArchivalGroupTypeHeader())
        {
            return await GetPopulatedArchivalGroup(uri, null, transaction);
        }
        else if(headResponse.HasBinaryTypeHeader())
        {
            return await GetObject<Binary>(uri, transaction);
        }
        else if (headResponse.HasBasicContainerTypeHeader())
        {
            // this is also true for archival group so test this last
            return await GetPopulatedContainer(uri, false, transaction, null, false);
        }
        return null;
    }

    public async Task<T?> GetObject<T>(Uri uri, Transaction? transaction = null) where T : Resource
    {
        var isBinary = typeof(T) == typeof(Binary);
        var reqUri = isBinary ? uri.MetadataUri() : uri;
        var request = MakeHttpRequestMessage(reqUri, HttpMethod.Get)
            .ForJsonLd(); 
        var response = await httpClient.SendAsync(request);

        if (isBinary)
        {
            var fileResponse = await MakeFedoraResponse<BinaryMetadataResponse>(response);
            var binary = new Binary(fileResponse)
            {
                Location = fileResponse.Id,
                FileName = fileResponse.FileName,
                Size = Convert.ToInt64(fileResponse.Size),
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
                if (response.HasArchivalGroupTypeHeader())
                {
                    var ag = new ArchivalGroup(directoryResponse) { Location = directoryResponse.Id };
                    return ag as T;
                }
                else
                {
                    var container = new Container(directoryResponse) { Location = directoryResponse.Id };
                    return container as T;
                }
            }
        }
        return null;
    }

    public async Task<ArchivalGroup?> GetPopulatedArchivalGroup(string path, string? version = null, Transaction? transaction = null)
    {
        var uri = new Uri(httpClient.BaseAddress!, path);
        return await GetPopulatedArchivalGroup(uri, version, transaction);
    }


    public async Task<ArchivalGroup?> GetPopulatedArchivalGroup(Uri uri, string? version = null, Transaction? transaction = null)
    {
        var versions = await GetFedoraVersions(uri);
        var storageMap = await storageMapper.GetStorageMap(uri, version);
        MergeVersions(versions, storageMap.AllVersions);
        ObjectVersion? objectVersion = null;
        if(!string.IsNullOrWhiteSpace(version))
        {
            objectVersion = versions.Single(v => v.MementoTimestamp == version || v.OcflVersion == version);
        }

        var archivalGroup = await GetPopulatedContainer(uri, true, transaction, objectVersion, true) as ArchivalGroup;
        if(archivalGroup == null)
        {
            return null;
        }
        if(archivalGroup.Location != uri)
        {
            throw new InvalidOperationException("location doesnt match uri");
        }

        archivalGroup.Origin = storageMapper.GetArchivalGroupOrigin(archivalGroup.Location);
        archivalGroup.Versions = versions;
        archivalGroup.StorageMap = storageMap;
        archivalGroup.Version = versions.Single(v => v.OcflVersion == storageMap.Version.OcflVersion);
        PopulateOrigins(storageMap, archivalGroup);
        return archivalGroup;
    }

    private void PopulateOrigins(StorageMap storageMap, Container container)
    {
        foreach(var binary in container.Binaries) 
        {
            if(storageMap.StorageType == "S3")
            {
                // This "s3-ness" needs to be inside an abstracted impl
                binary.Origin = $"s3://{storageMap.Root}/{storageMap.ObjectPath}/{storageMap.Hashes[binary.Digest!]}";
            }
        }
        foreach(var childContainer in container.Containers)
        {
            PopulateOrigins(storageMap, childContainer);
        }
    }

    private void MergeVersions(ObjectVersion[] fedoraVersions, ObjectVersion[] ocflVersions)
    {
        if(fedoraVersions.Length != ocflVersions.Length)
        {
            throw new InvalidOperationException("Fedora reports a different number of versions from OCFL");
        }
        for(int i = 0; i < fedoraVersions.Length; i++)
        {
            if(fedoraVersions[i].MementoTimestamp != ocflVersions[i].MementoTimestamp)
            {
                throw new InvalidOperationException($"Fedora reports a different MementoTimestamp {fedoraVersions[i].MementoTimestamp} from OCFL: {ocflVersions[i].MementoTimestamp}");
            }
            fedoraVersions[i].OcflVersion = ocflVersions[i].OcflVersion;
        }
    }

    private async Task<ObjectVersion[]> GetFedoraVersions(Uri uri)
    {
        var request = MakeHttpRequestMessage(uri.VersionsUri(), HttpMethod.Get)
            .ForJsonLd();

        var response = await httpClient.SendAsync(request);
        var content = await response.Content.ReadAsStringAsync();

        using (JsonDocument jDoc = JsonDocument.Parse(content))
        {
            List<string> childIds = GetIdsFromContainsProperty(jDoc.RootElement);
            // We could go and request each of these.
            // But... the Fedora API gives the created and lastmodified date of the original, not the version, when you ask for a versioned response.
            // Is that a bug?
            // We're not going to learn anything more than we would by parsing the memento path elements - which is TERRIBLY non-REST-y
            return childIds
                .Select(id => id.Split('/').Last())
                .Select(p => new ObjectVersion { MementoTimestamp = p, MementoDateTime = p.DateTimeFromMementoTimestamp() })
                .OrderBy(ov => ov.MementoTimestamp)
                .ToArray();
        }

    }


    private List<string> GetIdsFromContainsProperty(JsonElement element)
    {
        List<string> childIds = [];
        if (element.TryGetProperty("contains", out JsonElement contains))
        {
            if (contains.ValueKind == JsonValueKind.String)
            {
                childIds = [contains.GetString()!];
            }
            else if (contains.ValueKind == JsonValueKind.Array)
            {
                childIds = contains.EnumerateArray().Select(x => x.GetString()!).ToList();
            }
        }
        return childIds;
    }

    public async Task<Container?> GetPopulatedContainer(Uri uri, bool isArchivalGroup, Transaction? transaction = null, ObjectVersion? objectVersion = null, bool recurse = false)
    {

        // WithContainedDescriptions could return @graph or it could return a single object if the container has no children

        // https://uol.digirati.io/api/fedora/application/ld+json/storage-01/ocfl-expt-01-03-24-16-04-18/fcr:versions
        // =>
        // https://uol.digirati.io/api/fedora/application/ld+json/storage-01/ocfl-expt-01-03-24-16-04-18/fcr:versions/20240103160421
        // https://uol.digirati.io/api/fedora/application/ld+json/storage-01/ocfl-expt-01-03-24-16-04-18/fcr:versions/20240103160432
        // https://uol.digirati.io/api/fedora/application/ld+json/storage-01/ocfl-expt-01-03-24-16-04-18/fcr:versions/20240103160437
        // https://uol.digirati.io/api/fedora/application/ld+json/storage-01/ocfl-expt-01-03-24-16-04-18/fcr:versions/20240103160446

        // PROBLEM - I can't just append /fcr:version/20240103160421 because that causes an error if you also ask for .WithContainedDescriptions()
        // https://uol.digirati.io/api/fedora/application/ld+json/storage-01/ocfl-expt-01-03-24-16-04-18/fcr:versions

        // This is OK:
        // https://uol.digirati.io/api/fedora/application/ld+json/storage-01/ocfl-expt-01-03-24-16-04-18/fcr:versions/20240103160421
        // But not this
        // https://uol.digirati.io/api/fedora/application/ld+json/storage-01/ocfl-expt-01-03-24-16-04-18/fcr:versions/20240103160421?contained=true
        // Error: Unable to retrieve triples for info:fedora/storage-01/ocfl-expt-01-03-24-16-04-18/image.tiff

        // image.tiff is now a tombstone

        // Similarly:
        // https://uol.digirati.io/api/fedora/application/ld+json/storage-01/ocfl-expt-01-03-24-16-04-18/foo/fcr:versions/20240103160421?contained=true
        // This shows bar.xml (the single contained object) as having a size of 75 - but at timestamp 20240103160421 it didn't, as we can see:
        // https://uol.digirati.io/api/fedora/application/ld+json/storage-01/ocfl-expt-01-03-24-16-04-18/foo/bar.xml/fcr:metadata/fcr:versions/20240103160421

        // So the contained versions are (it seems) not asked for at the same version

        // I can't send the accept date header and also ask for withcontaineddescriptions IF one of them is now a tombstone
        // https://uol.digirati.io/api/fedora/application/ld+json/storage-01/ocfl-expt-01-03-24-16-04-18?acceptDate=20240103160421
        // https://uol.digirati.io/api/fedora/application/ld+json/storage-01/ocfl-expt-01-03-24-16-04-18?acceptDate=20240103160421&contained=true
        // 
        // I can't even do this:
        // https://uol.digirati.io/api/fedora/application/ld+json/storage-01/ocfl-expt-01-03-24-16-04-18/image.tiff/fcr:metadata?acceptDate=20240103160421
        // although I can ask for it directly
        // https://uol.digirati.io/api/fedora/application/ld+json/storage-01/ocfl-expt-01-03-24-16-04-18/image.tiff/fcr:metadata/fcr:versions/20240103160421

        // If I try to just ask for specific versions of everything in my traversal, that will fail:
        // OK because a specific version exists
        // https://uol.digirati.io/api/fedora/application/ld+json/storage-01/ocfl-expt-01-03-24-16-04-18/foo/bar.xml/fcr:metadata/fcr:versions/20240103160432
        // But not OK:
        // https://uol.digirati.io/api/fedora/application/ld+json/storage-01/ocfl-expt-01-03-24-16-04-18/foo/bar.xml/fcr:metadata/fcr:versions/20240103160437
        // Error: There is no version in info:fedora/storage-01/ocfl-expt-01-03-24-16-04-18/foo/bar.xml/fcr:metadata/fcr:versions/20240103160437 with a created date matching 2024-01-03T16:04:37Z


        // BUT I could use acceptDate to see that - 
        // https://uol.digirati.io/api/fedora/application/ld+json/storage-01/ocfl-expt-01-03-24-16-04-18/foo/bar.xml/fcr:metadata?acceptDate=20240103160437
        // ...because unlike image.tiff, the resource still exists

        // So, given all this info, how do I traverse an archival group to gather a specific VERSION?
        // Maybe we just don't support that; only the HEAD version, from this recursive walk, is available as a hierarchy of containers and binaries;
        // previous versions are only available as OCFL?

        // Or we have a much more expensive traversal, for versioned interactions only, that has to interact with each child binary individually
        // (can't rely on the efficiency of asking for contained descriptions). 


        // UPDATE - for now I will only return the HEAD object structure through the Fedora walk; if we want to access previous versions we will use the OCFL layout.
        // At some point though we should have a means of traversing the object via the Fedora API, no matter how slow, to verify that the file layout is the same as reported by OCFL.


        var request = MakeHttpRequestMessage(uri, HttpMethod.Get)
            .ForJsonLd()
            .WithContainedDescriptions();

        var response = await httpClient.SendAsync(request);
        bool hasArchivalGroupHeader = response.HasArchivalGroupTypeHeader();
        if (isArchivalGroup && !hasArchivalGroupHeader)
        {
            throw new InvalidOperationException("Response is not an Archival Group, when Archival Group expected");
        }
        if (!isArchivalGroup && hasArchivalGroupHeader)
        {
            throw new InvalidOperationException("Response is an Archival Group, when Basic Container expected");
        }

        var content = await response.Content.ReadAsStringAsync();

        using(JsonDocument jDoc = JsonDocument.Parse(content))
        {
            JsonElement[]? containerAndContained = null;
            if (jDoc.RootElement.TryGetProperty("@graph", out JsonElement graph))
            {
                containerAndContained = [.. graph.EnumerateArray()];
            }
            else
            {
                if (jDoc.RootElement.TryGetProperty("@id", out JsonElement idElement))
                {
                    containerAndContained = [jDoc.RootElement];
                }
            }

            if (containerAndContained == null || containerAndContained.Length == 0) 
            {
                throw new InvalidOperationException("Could not parse Archival Group");
            }
            if (containerAndContained[0].GetProperty("@id").GetString() != uri.ToString())
            {
                throw new InvalidOperationException("First resource in graph should be the asked-for URI");
            }

            // Make a map of the IDs
            Dictionary<string, JsonElement> dict = containerAndContained.ToDictionary(x => x.GetProperty("@id").GetString()!);
            var fedoraObject = JsonSerializer.Deserialize<FedoraJsonLdResponse>(containerAndContained[0]);
            Container topContainer;
            if(isArchivalGroup)
            {
                topContainer = new ArchivalGroup(fedoraObject) { Location = fedoraObject.Id };
            } 
            else
            {
                topContainer = new Container(fedoraObject) { Location = fedoraObject.Id };
            }
            // Get the contains property which may be a single value or an array
            List<string> childIds = GetIdsFromContainsProperty(containerAndContained[0]);
            foreach (var id in childIds)
            {
                var resource = dict[id];
                if (resource.HasType("fedora:Container"))
                {
                    var fedoraContainer = JsonSerializer.Deserialize<FedoraJsonLdResponse>(resource);
                    Container? container = null;
                    if (recurse)
                    {
                        container = await GetPopulatedContainer(fedoraContainer.Id, false, transaction, objectVersion, true);
                    }
                    else
                    {
                        container = new Container(fedoraContainer) { Location = fedoraContainer.Id }; 
                    }
                    topContainer.Containers.Add(container);
                }
                else if (resource.HasType("fedora:Binary"))
                {
                    var fedoraBinary = JsonSerializer.Deserialize<BinaryMetadataResponse>(resource);
                    var binary = new Binary(fedoraBinary)
                    {
                        Location = fedoraBinary.Id,
                    };
                    topContainer.Binaries.Add(binary);
                }
            }

            return topContainer;
        }
    }


    public async Task Delete(Uri uri, Transaction? transaction = null)
    {
        HttpRequestMessage req = MakeHttpRequestMessage(uri, HttpMethod.Delete)
            .InTransaction(transaction);

        var response = await httpClient.SendAsync(req);
        response.EnsureSuccessStatusCode();
    }

}
