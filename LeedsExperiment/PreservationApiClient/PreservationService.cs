﻿using Fedora.Abstractions;
using Preservation;
using System.Net.Http.Json;
using System.Text.Json;

namespace PreservationApiClient;

public class PreservationService : IPreservation
{
    private readonly HttpClient _httpClient;

    // This is horrible and wrong
    private const string repositoryPrefix = "api/repository/";
    private const string infoPrefix = "api/info/";
    private const string agPrefix = "api/archivalGroup/";
    private const string exportPrefix = "api/export/";
    private const string importPrefix = "api/import/";

    public PreservationService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public string GetInternalPath(Uri preservationApiUri)
    {
        return preservationApiUri.AbsolutePath.TrimStart('/').Replace(repositoryPrefix, string.Empty);
    }

    public async Task<Resource?> GetResource(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            path = string.Empty;
        }
        var req = new HttpRequestMessage(HttpMethod.Get, new Uri($"{repositoryPrefix}{path.TrimStart('/')}", UriKind.Relative));
        var response = await _httpClient.SendAsync(req);
        return await ParseResource(response);
    }

    private static async Task<Resource?> ParseResource(HttpResponseMessage response)
    {
        // This could be a Container, an ArchivalGroup, or a Binary
        var content = await response.Content.ReadAsStringAsync();

        using (JsonDocument jDoc = JsonDocument.Parse(content))
        {
            if (jDoc.RootElement.TryGetProperty("type", out JsonElement typeValue))
            {
                string type = typeValue.ToString();
                switch (type)
                {
                    case "Container":
                    case "RepositoryRoot":
                        return JsonSerializer.Deserialize<Container>(jDoc.RootElement);
                    case "Binary":
                        return JsonSerializer.Deserialize<Binary>(jDoc.RootElement);
                    case "ArchivalGroup":
                        return JsonSerializer.Deserialize<ArchivalGroup>(jDoc.RootElement);
                    default:
                        return null;
                }
            }
        }
        return null;
    }

    public async Task<ArchivalGroup?> GetArchivalGroup(string path, string? version)
    {
        var apiPath = $"{agPrefix}{path.TrimStart('/')}";
        if(!string.IsNullOrWhiteSpace(version))
        {
            apiPath += "?version=" + version;
        }
        var agApi = new Uri(apiPath, UriKind.Relative);
        var ag = await _httpClient.GetFromJsonAsync<ArchivalGroup>(agApi);
        return ag;
    }

    public async Task<ResourceInfo> GetResourceInfo(string path)
    {
        var apiPath = $"{infoPrefix}{path.TrimStart('/')}";
        var infoApi = new Uri(apiPath, UriKind.Relative);
        var info = await _httpClient.GetFromJsonAsync<ResourceInfo>(infoApi);
        return info!;
    }

    public async Task<ExportResult> Export(string path, string? version)
    {
        var apiPath = $"{exportPrefix}{path.TrimStart('/')}";
        if (!string.IsNullOrWhiteSpace(version))
        {
            apiPath += "?version=" + version;
        }
        var exportApi = new Uri(apiPath, UriKind.Relative);
        var exportResponse = await _httpClient.PostAsync(exportApi, null); // POST but no request body
        exportResponse.EnsureSuccessStatusCode();
        var export = await exportResponse.Content.ReadFromJsonAsync<ExportResult>();

        // What's the best way to deal with problems here?
        if (export != null)
        {
            return export;
        }
        throw new InvalidOperationException("Could not get an export object back");        
    }

    public async Task<ImportJob> GetUpdateJob(string archivalGroupPath, string source)
    {
        var apiPath = $"{importPrefix}{archivalGroupPath.TrimStart('/')}?source={source}";
        var importApi = new Uri(apiPath, UriKind.Relative);
        var importJob = await _httpClient.GetFromJsonAsync<ImportJob>(importApi);
        // What's the best way to deal with problems here?
        if (importJob != null)
        {
            return importJob;
        }
        throw new InvalidOperationException("Could not get an import object back");
    }

    public async Task<ImportJob> Import(ImportJob importJob)
    {
        var apiPath = $"{importPrefix}__import";
        var response = await _httpClient.PostAsJsonAsync(new Uri(apiPath, UriKind.Relative), importJob);
        var processedImportJob = await response.Content.ReadFromJsonAsync<ImportJob>();
        if (processedImportJob != null)
        {
            return processedImportJob;
        }
        throw new InvalidOperationException("Could not get a processed import object back");
    }


    public async Task<Container> CreateContainer(string path)
    {
        // This PUT is a bit too general
        var req = new HttpRequestMessage(HttpMethod.Put, new Uri($"{repositoryPrefix}{path.TrimStart('/')}", UriKind.Relative));
        var response = await _httpClient.SendAsync(req);
        var container = (await ParseResource(response)) as Container;
        if(container == null)
        {
            throw new InvalidOperationException("Resource is not a container");
        }
        return container;
    }
}
