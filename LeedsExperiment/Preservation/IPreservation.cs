using Fedora;
using Fedora.Abstractions;

namespace Preservation;

public interface IPreservation
{
    Task<Resource?> GetResource(string? path);
    string GetInternalPath(Uri preservationApiUri);
}
