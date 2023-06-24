using Amazon.S3.Model;

namespace WebScraperFileDownload.Services
{
    public interface IAmazonService
    {
        Task<PutObjectResponse> UploadToS3Bucket(Byte[] buffer, String fileName, String bucketName, String filePrefixPattern);
        String GetBucketNameFromEnvironmentVariable(String envVarName);
    }
}
