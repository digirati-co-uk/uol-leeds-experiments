using Fedora.Abstractions;

namespace Dashboard.Helpers;

public static class ResourceX
{
    public static string GetDisplayName(this Resource resource)
    {
        if(!string.IsNullOrWhiteSpace(resource.Name)) return resource.Name;

        if (resource.Type == "RepositoryRoot") return "(Root of repository)";

        var slug = resource.GetSlug();
        if(!string.IsNullOrWhiteSpace(slug)) return $"[{slug}]";

        return $"[({resource.GetType().Name})]";
    }

}
