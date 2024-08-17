
namespace Storage
{
    // Probably want this somewhere else, and also use this stuff throughout codebase
    public static class UriX
    {
        public static Uri Parent(this Uri uri)
        {
            return new Uri(uri.AbsoluteUri.Remove(uri.AbsoluteUri.Length - uri.Segments.Last().Length - uri.Query.Length).TrimEnd('/'));
        }

        public static string Slug(this Uri uri)
        {
            var uriSegments = uri.IsAbsoluteUri ? uri.Segments : uri.OriginalString.Split('/');
            return uriSegments.Last().Trim('/');
        }
    }
}
