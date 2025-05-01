using risk.control.system.Models.ViewModel;

namespace risk.control.system.Services
{
    public interface IClaimsAgentService
    {
        Task<AppiCheckifyResponse> PostAgentId(string userEmail, long locationId, long claimId, long faceId, string latitude, string longitude,bool isAgent, byte[]? image = null);
        Task<AppiCheckifyResponse> PostFaceId(string userEmail, long locationId, long claimId, long faceId, string latitude, string longitude, byte[]? image = null);
        Task<AppiCheckifyResponse> PostDocumentId(string userEmail, long locationId, long claimId, long docId, string latitude, string longitude, byte[]? image = null);
    }
    public class ClaimsAgentService : IClaimsAgentService
    {
        private readonly IAgentIdService agentIdService;

        public ClaimsAgentService(IAgentIdService agentIdService)
        {
            this.agentIdService = agentIdService;
        }
        public async Task<AppiCheckifyResponse> PostDocumentId(string userEmail, long locationId, long claimId, long docId, string latitude, string longitude, byte[]? image = null)
        {
            var locationLongLat = string.IsNullOrWhiteSpace(latitude) || string.IsNullOrWhiteSpace(longitude) ? string.Empty : $"{latitude}/{longitude}";

            var data = new DocumentData
            {
                LocationId = locationId,
                DocId = docId,
                Email = userEmail,
                ClaimId = claimId,
                OcrImage = Convert.ToBase64String(image),
                OcrLongLat = locationLongLat
            };
            var result = await agentIdService.GetDocumentId(data);
            return result;
        }
        public async Task<AppiCheckifyResponse> PostFaceId(string userEmail, long locationId, long claimId, long faceId, string latitude, string longitude, byte[]? image = null)
        {
            var locationLongLat = string.IsNullOrWhiteSpace(latitude) || string.IsNullOrWhiteSpace(longitude) ? string.Empty : $"{latitude}/{longitude}";
            var data = new FaceData
            {
                LocationId = locationId,
                FaceId = faceId,
                Email = userEmail,
                ClaimId = claimId,
                LocationImage = Convert.ToBase64String(image),
                LocationLongLat = locationLongLat
            };
            var result = await agentIdService.GetFaceId(data);
            return result;
        }

        public async Task<AppiCheckifyResponse> PostAgentId(string userEmail,long locationId, long claimId, long faceId, string latitude, string longitude, bool isAgent, byte[]? image = null)
        {
            var locationLongLat = string.IsNullOrWhiteSpace(latitude) || string.IsNullOrWhiteSpace(longitude) ? string.Empty : $"{latitude}/{longitude}";
            var data = new FaceData
            {
                IsAgent = isAgent,
                LocationId = locationId,
                FaceId = faceId,
                Email = userEmail,
                ClaimId = claimId,
                LocationImage = Convert.ToBase64String(image),
                LocationLongLat = locationLongLat
            };
            var result = await agentIdService.GetAgentId(data);
            return result;
        }
    }
}
