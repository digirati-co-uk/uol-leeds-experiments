using Fedora.Abstractions;
using Preservation;
using System.Net.Http.Json;
using System.Text.Json;

namespace PreservationApiClient;

public class PreservationService : IPreservation
{
    private readonly HttpClient _httpClient;
    // This is horrible and wrong
    private const string repositoryPrefix = "api/repository/";
    private const string agPrefix = "api/archivalGroup/";

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

    public Task<ExportResult> Export(string path, string? version)
    {
        throw new NotImplementedException();
    }

    public Task<ImportJob> GetUpdateJob(string path, string source)
    {
        throw new NotImplementedException();
    }

    public Task<ImportJob> Import(ImportJob importJob)
    {
        throw new NotImplementedException();
    }
}
