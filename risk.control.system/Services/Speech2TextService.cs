using System.Net;

using Amazon.Rekognition.Model;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using Amazon.TranscribeService;
using Amazon.TranscribeService.Model;

using Microsoft.AspNetCore.Identity;

using risk.control.system.Models.ViewModel;

namespace risk.control.system.Services
{
    public interface ISpeech2TextService
    {
        Task<string> ConvertSpeech(Speech2TextData input);
    }
    internal class Speech2TextService : ISpeech2TextService
    {
        private readonly IAmazonS3 s3Client;
        private readonly IAmazonTranscribeService transcribeClient;

        public Speech2TextService(IAmazonS3 s3Client, IAmazonTranscribeService transcribeClient)
        {
            this.s3Client = s3Client;
            this.transcribeClient = transcribeClient;
        }
        public async Task<string> ConvertSpeech(Speech2TextData input)
        {
            string bucketName = "icheckify-bucket";
            string fileName = $"{Guid.NewGuid()}_{input.SpeechInputData.FileName}";
            string transcribedText = "";

            try
            {
                // Check if bucket exists, if not, create it
                if (!(await Amazon.S3.Util.AmazonS3Util.DoesS3BucketExistV2Async(s3Client, bucketName)))
                {
                    var putBucketRequest = new PutBucketRequest
                    {
                        BucketName = bucketName,
                        UseClientRegion = true
                    };
                    await s3Client.PutBucketAsync(putBucketRequest);
                }
                // 2. Upload file to S3
                using (var stream = input.SpeechInputData.OpenReadStream())
                {
                    var uploadRequest = new TransferUtilityUploadRequest
                    {
                        InputStream = stream,
                        Key = fileName,
                        BucketName = bucketName
                    };
                    var fileTransferUtility = new TransferUtility(s3Client);
                    await fileTransferUtility.UploadAsync(uploadRequest);
                }

                // 3. Start Transcription Job
                var jobName = $"Job_{Guid.NewGuid()}";
                var startRequest = new StartTranscriptionJobRequest
                {
                    TranscriptionJobName = jobName,
                    LanguageCode = LanguageCode.EnUS,
                    MediaSampleRateHertz = 44100,
                    MediaFormat = MediaFormat.Mp3, // or Wav depending on input
                    Media = new Media { MediaFileUri = $"s3://{bucketName}/{fileName}" }
                };

                await transcribeClient.StartTranscriptionJobAsync(startRequest);

                // 4. Poll for completion (Simple loop for demo purposes)
                TranscriptionJob status;
                do
                {
                    await Task.Delay(2000); // Wait 2 seconds
                    var getRequest = new GetTranscriptionJobRequest { TranscriptionJobName = jobName };
                    var response = await transcribeClient.GetTranscriptionJobAsync(getRequest);
                    status = response.TranscriptionJob;
                } while (status.TranscriptionJobStatus == TranscriptionJobStatus.IN_PROGRESS);

                if (status.TranscriptionJobStatus == TranscriptionJobStatus.COMPLETED)
                {
                    // 5. Download the result JSON
                    using (var webClient = new WebClient())
                    {
                        var jsonResult = webClient.DownloadString(status.Transcript.TranscriptFileUri);
                        // Simple parsing (you might want to use System.Text.Json for complex results)
                        dynamic result = Newtonsoft.Json.JsonConvert.DeserializeObject(jsonResult);
                        transcribedText = result.results.transcripts[0].transcript;
                    }
                }
            }
            catch (Exception ex)
            {
                transcribedText = "Error during transcription: " + ex.Message;
            }
            return transcribedText;
        }
    }
}
