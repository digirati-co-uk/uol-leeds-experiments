namespace Utils
{
    public class S3Util
    {
        public static string GetAwsConsoleUri(string? fileUri)
        {
            if (fileUri == null) return string.Empty;

            const string template = "https://s3.console.aws.amazon.com/s3/object/{0}?region=eu-west-1&bucketType=general&prefix={1}";
            var bucketAndKey = fileUri.RemoveStart("s3://");
            var bucket = bucketAndKey.Split('/')[0];
            var key = bucketAndKey.Substring(bucket.Length + 1);
            return string.Format(template, bucket, key);
        }
    }
}
