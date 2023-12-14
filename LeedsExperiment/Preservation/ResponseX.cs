using Fedora.Vocab;

namespace Preservation
{
    public static class ResponseX
    {
        public static bool HasArchivalGroupHeader(this HttpResponseMessage response)
        {
            // "Link", $"<{RepositoryTypes.ArchivalGroup}>;rel=\"type\""

            // This could be nicer
            if(response.Headers.TryGetValues("Link", out IEnumerable<string>? values))
            {
                if(values.Any(v => v.Contains(RepositoryTypes.ArchivalGroup)))
                {
                    return true;
                }
            }
            return false;

        }
    }
}
