using System.IO.Compression;
using Amazon.S3;
using Amazon.S3.Model;
using risk.control.system.AppConstant;
using risk.control.system.Helpers;

namespace risk.control.system.Services.AgencyAdmin
{
    public interface IAwsS3Service
    {
        Task<bool> ZipAndUploadToS3Async(string sourceFolder, string s3Key);
    }
    internal class AwsS3Service(
        IAmazonS3 s3Client,
        ILogger<AwsS3Service> logger
        ) : IAwsS3Service
    {
        private readonly IAmazonS3 _s3Client = s3Client;
        private readonly ILogger<AwsS3Service> _logger = logger;

        public async Task<bool> ZipAndUploadToS3Async(string sourceFolder, string s3Key)
        {
            try
            {
                if (!(await Amazon.S3.Util.AmazonS3Util.DoesS3BucketExistV2Async(_s3Client, EnvHelper.Get(CONSTANTS.S3_BUCKET))))
                {
                    var putBucketRequest = new PutBucketRequest { BucketName = EnvHelper.Get(CONSTANTS.S3_BUCKET), UseClientRegion = true };

                    await _s3Client.PutBucketAsync(putBucketRequest);

                    var publicAccessBlockRequest = new PutPublicAccessBlockRequest
                    {
                        BucketName = EnvHelper.Get(CONSTANTS.S3_BUCKET),
                        PublicAccessBlockConfiguration = new PublicAccessBlockConfiguration
                        {
                            BlockPublicAcls = true,
                            BlockPublicPolicy = true,
                            IgnorePublicAcls = true,
                            RestrictPublicBuckets = true
                        }
                    };
                    await _s3Client.PutPublicAccessBlockAsync(publicAccessBlockRequest);
                }

                await using var memoryStream = new MemoryStream();

                ZipFile.CreateFromDirectory(sourceFolder, memoryStream, CompressionLevel.Optimal, includeBaseDirectory: false);
                memoryStream.Position = 0;

                var putRequest = new PutObjectRequest
                {
                    BucketName = EnvHelper.Get(CONSTANTS.S3_BUCKET),
                    Key = s3Key,
                    InputStream = memoryStream,
                    ContentType = CONSTANTS.ZIP_CONTENT_TYPE,
                };

                PutObjectResponse response = await _s3Client.PutObjectAsync(putRequest);
                return (response.HttpStatusCode == System.Net.HttpStatusCode.OK);
            }
            catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.PreconditionFailed)
            {
                // The object already exists, handle accordingly
                _logger.LogError(ex, "Error occurred while zipping and uploading to S3.");
                return false;
            }
            catch (Exception ex)
            {
                // Log the exception or handle it as needed
                _logger.LogError(ex, "Error occurred while zipping and uploading to S3.");
                return false;
            }
        }
    }
}
