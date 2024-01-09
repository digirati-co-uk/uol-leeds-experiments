using Fedora.ApiModel;
using Fedora.Storage;
using System.Text.Json.Serialization;

namespace Fedora;

public class ArchivalGroup : Container
{
    public ArchivalGroup(FedoraJsonLdResponse fedoraResponse) : base(fedoraResponse)
    {
        Type = "ArchivalGroup";
    }

    [JsonPropertyName("version")]
    [JsonPropertyOrder(2)]
    public ObjectVersion? Version { get; set; }

    [JsonPropertyName("versions")]
    [JsonPropertyOrder(3)]
    public ObjectVersion[]? Versions { get; set; }

    [JsonPropertyName("storageMap")]
    [JsonPropertyOrder(101)]
    public StorageMap? StorageMap { get; set; }

    public Uri GetResourceUri(string path)
    {
        // the Location property won't end with a trailing slash, so we can't create URIs with the normal Uri constructor
        // we can't do:
        // new Uri(Location, "foo/bar.xml");
        // and nor can we use "./foo/bar.xml" or "/foo/bar.xml" 
        if(Location == null)
        {
            throw new InvalidOperationException("Needs a location");
        }
        if(Location.AbsolutePath.EndsWith("/"))
        {
            // I'm pretty sure this will never be the case
            return new Uri(Location, path);
        }
        return new Uri($"{Location}/{path}");
    }

}
