using System.Diagnostics.CodeAnalysis;

namespace Utils;

public static class ArchivalGroupUriHelpers
{
    [return: NotNullIfNotNull(nameof(u))]
    public static Uri? GetArchivalGroupRelativePath(Uri? u) =>
        u == null ? null : new(GetArchivalGroupPath(u), UriKind.Relative);

    public static string GetArchivalGroupPath(Uri u) =>
        GetArchivalGroupPath(u.IsAbsoluteUri ? u.AbsolutePath : u.OriginalString);

    public static string GetArchivalGroupPath(string path)
    {
        // this feels wrong - we should be using consistent paths.
        // Former is Preservation-Api path, latter is Fedora path. If it's anything else just use that
        const string preservationApiPrefix = "/repository";
        const string fedoraPrefix = "/fcrepo/rest";
        const string storageApiPrefix = "/api";

        foreach (var s in new[] { preservationApiPrefix, fedoraPrefix, storageApiPrefix })
        {
            if (path.StartsWith(s, StringComparison.OrdinalIgnoreCase))
            {
                path = path.Replace(s, string.Empty);
                break;
            }
        }

        return path[0] == '/' ? path[1..] : path;
    }
}