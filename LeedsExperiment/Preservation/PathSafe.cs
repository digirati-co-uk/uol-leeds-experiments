using System.Net;

namespace Preservation
{
    public class PathSafe
    {
        public static bool ValidSlug(string slug)
        {
            return WebUtility.UrlEncode(slug) == slug;
        }


        public static bool ValidPath(string path)
        {
            var parts = path.Split('/');
            return parts.All(ValidSlug);
        }
    }
}
