using Amazon.Polly;
using Amazon.Polly.Model;

namespace risk.control.system.Services.Tool
{
    public interface IText2SpeechService
    {
        Task<byte[]> Convert(string text);
    }
    internal class Text2SpeechService(IAmazonPolly client) : IText2SpeechService
    {
        private readonly IAmazonPolly _client = client;

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
                var response = await _client.SynthesizeSpeechAsync(request);

                using (var memoryStream = new MemoryStream())
                {
                    // Copy the audio stream to a memory stream
                    await response.AudioStream.CopyToAsync(memoryStream);
                    return memoryStream.ToArray();
                }
            }
            catch (Exception)
            {
                // Log your error here
                return [];
            }
        }
    }
}
