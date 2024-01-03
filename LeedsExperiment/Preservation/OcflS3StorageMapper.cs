using Amazon.S3;
using Fedora;
using Fedora.Storage;
using Microsoft.Extensions.Options;

namespace Preservation
{
    public class OcflS3StorageMapper : IStorageMapper
    {
        private IAmazonS3 s3Client;
        private FedoraAwsOptions fedoraAws;

        public OcflS3StorageMapper(IAmazonS3 s3Client, IOptions<FedoraAwsOptions> options)
        {
            this.s3Client = s3Client;
            fedoraAws = options.Value;
        }

        public async Task<StorageMap> GetStorageMap(Uri archivalGroupUri, string? version = null)
        {
            // an inventory:
            // https://s3.console.aws.amazon.com/s3/object/uol-expts-fedora-01?region=eu-west-1&prefix=453/b09/4bb/453b094bb0ba9a3866ebe2c553b125dde44f4e12147265b92f94379935e248dd/inventory.json

            // Create a new multi-version one too and use that
            // Make the .NET classes

            // From the URI get the S3 location
            // Load the inventory
            // make sure it's an archival group (how)
            // 
            // Deserialise to strongly typed .net classes

            // Extra
            // in the "web app" list the archival objects in a root basic container.
            return null;
        }
    }
}
