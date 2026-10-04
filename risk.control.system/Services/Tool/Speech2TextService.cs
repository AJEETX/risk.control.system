using System.Text.Json;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using Amazon.TranscribeService;
using Amazon.TranscribeService.Model;
using Microsoft.EntityFrameworkCore;
using risk.control.system.AppConstant;
using risk.control.system.Helpers;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;
namespace risk.control.system.Services.Tool
{
    public interface ISpeech2TextService
    {
        Task<string> ConvertSpeech(Speech2TextData input);
        Task ConvertMediaSpeech(string relativePath, long mediaId);
    }

    internal class Speech2TextService(IAmazonS3 s3Client, IAmazonTranscribeService transcribeClient, IHttpClientFactory clientFactory, ApplicationDbContext context, ILogger<Speech2TextService> logger) : ISpeech2TextService
    {
        private readonly string bucketName = EnvHelper.Get(CONSTANTS.S3_BUCKET)!;
        private readonly IAmazonS3 _s3Client = s3Client;
        private readonly IAmazonTranscribeService _transcribeClient = transcribeClient;
        private readonly IHttpClientFactory _clientFactory = clientFactory;
        private readonly ApplicationDbContext _context = context;
        private readonly ILogger<Speech2TextService> _logger = logger;

        public async Task ConvertMediaSpeech(string relativePath, long mediaId)
        {
            // 1. Validate file existence and extract filename
            if (string.IsNullOrWhiteSpace(relativePath) || !File.Exists(relativePath))
            {
                _logger.LogError("File not found at path: {FilePath} for MediaId: {MediaId}", relativePath, mediaId);
                return;
            }

            string originalFileName = Path.GetFileName(relativePath);
            string fileName = $"{Guid.NewGuid()}_{originalFileName}";
            string transcribedText = "No data available";

            try
            {
                // 2. Ensure S3 Bucket Setup
                if (!(await Amazon.S3.Util.AmazonS3Util.DoesS3BucketExistV2Async(_s3Client, bucketName)))
                {
                    var putBucketRequest = new PutBucketRequest { BucketName = bucketName, UseClientRegion = true };
                    await _s3Client.PutBucketAsync(putBucketRequest);

                    var publicAccessBlockRequest = new PutPublicAccessBlockRequest
                    {
                        BucketName = bucketName,
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

                // 3. Upload local file stream to S3
                await using (var stream = File.OpenRead(relativePath))
                {
                    var uploadRequest = new TransferUtilityUploadRequest
                    {
                        InputStream = stream,
                        Key = fileName,
                        BucketName = bucketName
                    };
                    var fileTransferUtility = new TransferUtility(_s3Client);
                    await fileTransferUtility.UploadAsync(uploadRequest);
                }

                // 4. Start & Monitor AWS Transcribe Job
                var jobName = $"Job_{Guid.NewGuid()}";
                var startRequest = CreateRequest(jobName, fileName);
                await _transcribeClient.StartTranscriptionJobAsync(startRequest);

                TranscriptionJob status;
                do
                {
                    await Task.Delay(2000); // Poll every 2 seconds
                    var getRequest = new GetTranscriptionJobRequest { TranscriptionJobName = jobName };
                    var response = await _transcribeClient.GetTranscriptionJobAsync(getRequest);
                    status = response.TranscriptionJob;
                }
                while (status.TranscriptionJobStatus == TranscriptionJobStatus.IN_PROGRESS
                    || status.TranscriptionJobStatus == TranscriptionJobStatus.QUEUED);

                // 5. Retrieve Transcript or Handle Failure
                if (status.TranscriptionJobStatus == TranscriptionJobStatus.COMPLETED)
                {
                    var httpClient = _clientFactory.CreateClient();
                    try
                    {
                        var result = await httpClient.GetFromJsonAsync<JsonElement>(status.Transcript.TranscriptFileUri);
                        transcribedText = result.GetProperty("results").GetProperty("transcripts")[0].GetProperty("transcript").GetString()!;
                    }
                    catch (HttpRequestException e)
                    {
                        _logger.LogError(e, "Error fetching transcript JSON from AWS S3 for Job {JobName}", jobName);
                    }
                }
                else if (status.TranscriptionJobStatus == TranscriptionJobStatus.FAILED)
                {
                    _logger.LogError("Transcription Job {JobName} failed: {FailureReason}", jobName, status.FailureReason);
                    transcribedText = $"Transcription failed: {status.FailureReason}";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing speech-to-text for MediaId {MediaId}", mediaId);
                transcribedText = "Error during transcription: " + ex.Message;
            }
            finally
            {
                // 6. Cleanup S3 temporary file to prevent unnecessary storage costs
                try
                {
                    var deleteRequest = new DeleteObjectRequest
                    {
                        BucketName = bucketName,
                        Key = fileName
                    };
                    await _s3Client.DeleteObjectAsync(deleteRequest);
                }
                catch (Exception s3Ex)
                {
                    _logger.LogWarning(s3Ex, "Failed to clean up temporary S3 file {FileName} from bucket {BucketName}", fileName, bucketName);
                }
            }

            // 7. Update Database Record
            var media = await _context.MediaReport.FirstOrDefaultAsync(m => m.Id == mediaId);
            if (media != null)
            {
                media.Transcript = transcribedText;
                await _context.SaveChangesAsync();
            }
        }
        public async Task<string> ConvertSpeech(Speech2TextData input)
        {
            string fileName = $"{Guid.NewGuid()}_{input.SpeechInputData!.FileName}";
            string transcribedText = "";
            try
            {
                if (!(await Amazon.S3.Util.AmazonS3Util.DoesS3BucketExistV2Async(_s3Client, bucketName)))
                {
                    var putBucketRequest = new PutBucketRequest { BucketName = bucketName, UseClientRegion = true };
                    await _s3Client.PutBucketAsync(putBucketRequest);
                    var publicAccessBlockRequest = new PutPublicAccessBlockRequest
                    {
                        BucketName = bucketName,
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
                await using (var stream = input.SpeechInputData.OpenReadStream())
                {
                    var uploadRequest = new TransferUtilityUploadRequest { InputStream = stream, Key = fileName, BucketName = bucketName };
                    var fileTransferUtility = new TransferUtility(_s3Client);
                    await fileTransferUtility.UploadAsync(uploadRequest);
                }
                var jobName = $"Job_{Guid.NewGuid()}";
                var startRequest = CreateRequest(jobName, fileName);
                await _transcribeClient.StartTranscriptionJobAsync(startRequest);
                TranscriptionJob status;
                do
                {
                    await Task.Delay(2000); // Wait 2 seconds
                    var getRequest = new GetTranscriptionJobRequest { TranscriptionJobName = jobName };
                    var response = await _transcribeClient.GetTranscriptionJobAsync(getRequest);
                    status = response.TranscriptionJob;
                } while (status.TranscriptionJobStatus == TranscriptionJobStatus.IN_PROGRESS);

                if (status.TranscriptionJobStatus == TranscriptionJobStatus.COMPLETED)
                {
                    var httpClient = _clientFactory.CreateClient();
                    try
                    {
                        var result = await httpClient.GetFromJsonAsync<JsonElement>(status.Transcript.TranscriptFileUri);
                        transcribedText = result.GetProperty("results").GetProperty("transcripts")[0].GetProperty("transcript").GetString()!;
                    }
                    catch (HttpRequestException e)
                    {
                        Console.WriteLine($"Request error: {e.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                transcribedText = "Error during transcription: " + ex.Message;
            }
            finally
            {
                // 6. Cleanup S3 temporary file to prevent unnecessary storage costs
                try
                {
                    var deleteRequest = new DeleteObjectRequest
                    {
                        BucketName = bucketName,
                        Key = fileName
                    };
                    await _s3Client.DeleteObjectAsync(deleteRequest);
                }
                catch (Exception s3Ex)
                {
                    _logger.LogWarning(s3Ex, "Failed to clean up temporary S3 file {FileName} from bucket {BucketName}", fileName, bucketName);
                }
            }
            return transcribedText;
        }
        private StartTranscriptionJobRequest CreateRequest(string jobName, string fileName)
        {
            var startRequest = new StartTranscriptionJobRequest
            {
                TranscriptionJobName = jobName,
                LanguageCode = LanguageCode.EnUS,
                Media = new Media { MediaFileUri = $"s3://{bucketName}/{fileName}" }
            };
            return startRequest;
        }
    }
}