using Amazon.S3;
using Amazon.S3.Model;
using Fedora;
using Fedora.Storage;
using Fedora.Storage.Ocfl;
using Fedora.Vocab;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace Preservation
{
    public class OcflS3StorageMapper : IStorageMapper
    {
        private IAmazonS3 s3Client;
        private FedoraAwsOptions fedoraAws;
        private FedoraApiOptions fedoraApi;

        public OcflS3StorageMapper(
            IAmazonS3 awsS3Client, 
            IOptions<FedoraAwsOptions> fedoraAwsOptions,
            IOptions<FedoraApiOptions> fedoraApiOptions)
        {
            s3Client = awsS3Client;
            fedoraAws = fedoraAwsOptions.Value;
            fedoraApi = fedoraApiOptions.Value;
        }

        public async Task<StorageMap> GetStorageMap(Uri archivalGroupUri, string? version = null)
        {
            var agOrigin = GetArchivalGroupOrigin(archivalGroupUri);

            var invReq = new GetObjectRequest { BucketName = fedoraAws.Bucket, Key = $"{agOrigin}/inventory.json" };
            var invResp = await s3Client.GetObjectAsync(invReq);
            var inventory = JsonSerializer.Deserialize<Inventory>(invResp.ResponseStream);
            var inventoryVersions = inventory!.Versions
                .Select(kvp => new ObjectVersion
                {
                    OcflVersion = kvp.Key,
                    MementoDateTime = kvp.Value.Created,
                    MementoTimestamp = kvp.Value.Created.ToMementoTimestamp(),
                })
               .OrderBy(o => o.MementoDateTime)
               .ToList();

            if(version == null)
            {
                // Use the latest version
                version = inventory!.Head!;
            }

            // Allow the supplied string to be either ocfl vX or memento timestamp (they cannot overlap!)
            ObjectVersion objectVersion = inventoryVersions.Single(v => v.OcflVersion == version || v.MementoTimestamp == version);

            var mapFiles = new Dictionary<string, OriginFile>();
            var ocflVersion = inventory.Versions[objectVersion.OcflVersion!];
            foreach(var kvp in ocflVersion.State) 
            {
                var files = kvp.Value.Where(f => !IsFedoraMetadata(f)).ToList();
                if(files.Count > 0)
                {
                    var hash = kvp.Key;
                    var actualPath = inventory.Manifest[hash][0];// I don't think there'll ever be more than one entry in a Fedora instance - see https://ocfl.io/1.1/spec/#manifest 
                    var originFile = new OriginFile 
                    { 
                        Hash = hash, 
                        FullPath = actualPath 
                    };
                    foreach(var file in files)
                    {
                        mapFiles[file] = originFile;
                    }
                }
            }

            // Validate that the OCFL layout thinks this is an Archival Group
            var rootInfoKey = $"{agOrigin}/{objectVersion.OcflVersion}/content/.fcrepo/fcr-root.json";
            var rootInfoReq = new GetObjectRequest { BucketName = fedoraAws.Bucket, Key = rootInfoKey };
            var rootInfoinvResp = await s3Client.GetObjectAsync(rootInfoReq);

            bool? archivalGroup = null;
            bool? objectRoot = null;
            bool? deleted = null;
            using (JsonDocument jDoc = JsonDocument.Parse(rootInfoinvResp.ResponseStream))
            {
                if (jDoc.RootElement.TryGetProperty("archivalGroup", out JsonElement jArchivalGroup))
                {
                    archivalGroup = jArchivalGroup.GetBoolean();
                }
                if (jDoc.RootElement.TryGetProperty("objectRoot", out JsonElement jObjectRoot))
                {
                    objectRoot = jObjectRoot.GetBoolean();
                }
                if (jDoc.RootElement.TryGetProperty("deleted", out JsonElement jDeleted))
                {
                    deleted = jDeleted.GetBoolean();
                }
            }

            if (
                archivalGroup.HasValue && archivalGroup == true && 
                objectRoot.HasValue && objectRoot == true && 
                deleted.HasValue && deleted == false)
            {
                return new StorageMap()
                {
                    Version = objectVersion,
                    StorageType = "S3",
                    Root = fedoraAws.Bucket,
                    ObjectPath = agOrigin!,
                    AllVersions = inventoryVersions.ToArray(),
                    Files = mapFiles
                };
            }
            else
            {
                throw new InvalidOperationException("Not an archival object");
            }

        }

        private bool IsFedoraMetadata(string filepath)
        {
            if (
                filepath.StartsWith(".fcrepo/")       ||
                filepath.EndsWith("fcr-container.nt") ||
                filepath.EndsWith("~fcr-desc.nt")     || 
                filepath.EndsWith("~fcr-acl.nt")
            )
            {
                return true;
            }
            return false;
        }

        public string? GetArchivalGroupOrigin(Uri archivalGroupUri)
        {
            var uriString = archivalGroupUri.ToString();
            if (!uriString.StartsWith(fedoraApi.ApiRoot))
            {
                return null;
            }
            var idPart = uriString.Remove(0, fedoraApi.ApiRoot.Length);
            return RepositoryPath.RelativeToRoot(idPart);
        }
    }
}
