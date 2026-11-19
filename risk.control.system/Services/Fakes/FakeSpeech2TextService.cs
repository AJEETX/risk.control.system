using Microsoft.EntityFrameworkCore;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;
using risk.control.system.Services.Tool;

namespace risk.control.system.Services.Fakes
{
    /// <summary>
    /// Development/E2E stand-in for AWS S3 + Transcribe: writes a canned transcript instead of uploading the media.
    /// Registered only when USE_FAKE_EXTERNAL_SERVICES=true in Development.
    /// </summary>
    internal sealed class FakeSpeech2TextService(ApplicationDbContext context, ILogger<FakeSpeech2TextService> logger) : ISpeech2TextService
    {
        public const string Transcript = "E2E transcript: the claimant confirmed the incident details at the site.";

        private readonly ApplicationDbContext _context = context;
        private readonly ILogger<FakeSpeech2TextService> _logger = logger;

        public async Task ConvertMediaSpeech(string relativePath, long mediaId)
        {
            var media = await _context.MediaReport.FirstOrDefaultAsync(m => m.Id == mediaId);
            if (media != null)
            {
                media.Transcript = Transcript;
                await _context.SaveChangesAsync();
            }

            _logger.LogInformation("[FAKE TRANSCRIBE] Media {MediaId} transcribed", mediaId);
        }

        public async Task ConvertAudioCallSpeech(string relativePath, long audioId)
        {
            var call = await _context.AgentCallRequests.FirstOrDefaultAsync(m => m.Id == audioId);
            if (call != null)
            {
                call.Transcript = Transcript;
                await _context.SaveChangesAsync();
            }
        }

        public Task<string> ConvertSpeech(Speech2TextData input) => Task.FromResult(Transcript);

        public Task<string> ConvertSpeech2Text(Speech2TextRequest input) => Task.FromResult(Transcript);
    }
}
