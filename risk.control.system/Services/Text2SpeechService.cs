using Amazon.Polly;
using Amazon.Polly.Model;

namespace risk.control.system.Services
{
    public interface IText2SpeechService
    {
        Task<byte[]> Convert(string text);
    }
    internal class Text2SpeechService : IText2SpeechService
    {
        private readonly IAmazonPolly client;

        public Text2SpeechService(IAmazonPolly client)
        {
            this.client = client;
        }
        public async Task<byte[]> Convert(string text)
        {
            var request = new SynthesizeSpeechRequest
            {
                Text = text,
                OutputFormat = OutputFormat.Mp3,
                VoiceId = VoiceId.Joanna // You can also use 'Matthew', 'Amy', etc.
            };

            try
            {
                var response = await client.SynthesizeSpeechAsync(request);

                using (var memoryStream = new MemoryStream())
                {
                    // Copy the audio stream to a memory stream
                    await response.AudioStream.CopyToAsync(memoryStream);
                    return memoryStream.ToArray();
                }
            }
            catch (Exception ex)
            {
                // Log your error here
                return new byte[0];
            }
        }
    }
}
