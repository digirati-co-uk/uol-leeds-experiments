using Fedora.Storage;

namespace Fedora;

public interface IStorageMapper
{
    Task<StorageMap> GetStorageMap(ArchivalGroup archivalGroup, string? version = null);
}
