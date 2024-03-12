using Amazon.S3;
using Amazon.S3.Model;

namespace Preservation;

public class AwsChecksum
{
    public static async Task<string?> GetHexChecksumAsync(IAmazonS3 s3Client, string bucket, string key)
    {
        var objAttrsRequest = new GetObjectAttributesRequest()
        {
            BucketName = bucket,
            Key = key,
            ObjectAttributes = [ObjectAttributes.Checksum]
        };
        var objAttrsResponse = await s3Client!.GetObjectAttributesAsync(objAttrsRequest);
        string? base64Sha256 = objAttrsResponse?.Checksum?.ChecksumSHA256;
        if (!string.IsNullOrWhiteSpace(base64Sha256))
        {
            byte[] bytes = Convert.FromBase64String(base64Sha256);
            return BitConverter.ToString(bytes).Replace("-", "").ToLowerInvariant();
            
        }
        return null;
    }
}
