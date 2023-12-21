using Fedora;
using Fedora.Storage;

namespace Preservation
{
    public class S3StorageMapper : IStorageMapper
    {
        public Task<StorageMap> GetStorageMap(ArchivalGroup archivalGroup, string? version = null)
        {
            throw new NotImplementedException();
        }
    }
}
