using System.Text.Json;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using Amazon.TranscribeService;
using Amazon.TranscribeService.Model;
using risk.control.system.Models.ViewModel;

namespace risk.control.system.Services.Tool
{
    public interface ISpeech2TextService
    {
        Task<string> ConvertSpeech(Speech2TextData input);
    }

    internal class Speech2TextService(IAmazonS3 s3Client, IAmazonTranscribeService transcribeClient, IHttpClientFactory clientFactory) : ISpeech2TextService
    {
        private string bucketName = "icheckify-bucket";
        private readonly IAmazonS3 _s3Client = s3Client;
        private readonly IAmazonTranscribeService _transcribeClient = transcribeClient;
        private readonly IHttpClientFactory _clientFactory = clientFactory;

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
                }
                using (var stream = input.SpeechInputData.OpenReadStream())
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
            return transcribedText;
        }
        private StartTranscriptionJobRequest CreateRequest(string jobName, string fileName)
        {
            var startRequest = new StartTranscriptionJobRequest
            {
                TranscriptionJobName = jobName,
                LanguageCode = LanguageCode.EnUS,
                MediaSampleRateHertz = 44100,
                MediaFormat = MediaFormat.Mp3,
                Media = new Media { MediaFileUri = $"s3://{bucketName}/{fileName}" }
            };
            return startRequest;
        }
    }
}