using Fedora;
using Fedora.Storage;

namespace Preservation
{
    public class OcflS3StorageMapper : IStorageMapper
    {
        public Task<StorageMap> GetStorageMap(Uri archivalGroupUri, string? version = null)
        {
            throw new NotImplementedException();
        }
    }
}
