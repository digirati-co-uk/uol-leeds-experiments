using Fedora.Storage;

namespace Fedora;

public interface IStorageMapper
{
    Task<StorageMap> GetStorageMap(Uri archivalGroupUri, string? version = null);
}
