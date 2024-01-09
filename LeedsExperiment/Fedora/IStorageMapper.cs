using Fedora.Storage;
using Fedora.Storage.Ocfl;

namespace Fedora;

public interface IStorageMapper
{
    Task<StorageMap> GetStorageMap(Uri archivalGroupUri, string? version = null);

    Task<Inventory?> GetInventory(Uri archivalGroupUri);

    string? GetArchivalGroupOrigin(Uri archivalGroupUri);
}
