using risk.control.system.Models.ViewModel;

namespace risk.control.system.Services
{
    public interface IClaimsAgentService
    {
        Task<AppiCheckifyResponse> PostFaceId(string userEmail, string claimId, string latitude, string longitude, byte[]? image = null);
        Task<AppiCheckifyResponse> PostAudio(string userEmail, string claimId, string latitude, string longitude, string filename, byte[]? image = null);
        Task<AppiCheckifyResponse> PostVideo(string userEmail, string claimId, string latitude, string longitude, string filename, byte[]? image = null);
        Task<AppiCheckifyResponse> PostDocumentId(string userEmail, string claimId, string latitude, string longitude, byte[]? image = null);
        Task<AppiCheckifyResponse> PostPassportId(string userEmail, string claimId, string latitude, string longitude, byte[]? image = null);
    }
}
