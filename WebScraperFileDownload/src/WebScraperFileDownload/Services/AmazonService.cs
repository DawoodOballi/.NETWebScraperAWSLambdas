using System.Diagnostics.CodeAnalysis;
using Amazon.Lambda.Core;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.DependencyInjection;

namespace WebScraperFileDownload.Services
{
    public class AmazonService : IAmazonService
    {
		private IAmazonS3 _s3Client;
        private PutObjectRequest _putObjectRequest;
        private ILambdaLogger _logger;

        public AmazonService(IAmazonS3 s3Client, PutObjectRequest putObjectRequest, ILambdaLogger logger)
		{
            ConfigureServices(s3Client, putObjectRequest, logger);
        }

        public String GetBucketNameFromEnvironmentVariable(String envVarName)
        {
            _logger.LogInformation("Reading Environment Variables");
            String? BUCKET_NAME = Environment.GetEnvironmentVariable(envVarName);
            // if(BUCKET_NAME == null)
            //     Environment.SetEnvironmentVariable(envVarName, "test-net-lambda");
            // BUCKET_NAME = Environment.GetEnvironmentVariable(envVarName);
            _logger.LogInformation("All environment variables have been retreieved");
            return BUCKET_NAME;
        }

        public async Task<PutObjectResponse> UploadToS3Bucket(Byte[] buffer, String fileName, String bucketName, String filePrefixPattern)
        {
            PutObjectResponse response;
            var encoding = new System.Text.UTF8Encoding();
            try
            {
                _logger.LogInformation("Setting up the PutObjectRequest to pass for upload");
                // Create a PutObject request
                _putObjectRequest.BucketName = bucketName;
                _putObjectRequest.Key = fileName+DateTime.Now.ToString(filePrefixPattern)+".csv";
                _putObjectRequest.ContentBody = encoding.GetString(buffer);

                _logger.LogInformation("Finished setting up the PutObjectRequest to pass for upload");

                _logger.LogInformation($"Uploading file to {bucketName}");
                // Put object
                response = await _s3Client.PutObjectAsync(_putObjectRequest);
            }
            catch (AmazonS3Exception ex)
            {   
                _logger.LogError("The bucket you are trying to upload to does not exist. -> Exception Type: \n " 
                + ex.GetType() + "\nMessage: " + ex.Message);
                throw;
            }
            catch (Exception ex)
            {   
                _logger.LogError("The bucket value cannot be null. -> Exception Type: \n " 
                + ex.GetType() + "\nMessage: " + ex.Message);
                throw;
            }
            _logger.LogInformation($"Successfully uploaded file {fileName+DateTime.Now.ToString(filePrefixPattern)+".csv"} to {bucketName}");
            return response;
        }

        
        [ExcludeFromCodeCoverage]
        private void ConfigureServices(IAmazonS3 s3Client = null, PutObjectRequest putObjectRequest = null, ILambdaLogger logger = null)
        {
            Startup.ConfigureServices();
            _s3Client = s3Client ?? Startup.Services.GetRequiredService<IAmazonS3>();
            _putObjectRequest = putObjectRequest ?? Startup.Services.GetRequiredService<PutObjectRequest>();
            _logger = logger ?? Startup.Services.GetRequiredService<ILambdaLogger>();

        }
    }
}
