using risk.control.system.Models.ViewModel;

namespace risk.control.system.Services
{
    public interface IClaimsAgentService
    {
        Task<AppiCheckifyResponse> PostAgentId(string userEmail, string reportName, string locationName, long locationId, long claimId, long faceId, string latitude, string longitude, bool isAgent, IFormFile Image);
        Task<AppiCheckifyResponse> PostDocumentId(string userEmail, string reportName, string locationName, long locationId, long claimId, long docId, string latitude, string longitude, IFormFile Image);
    }
    internal class ClaimsAgentService : IClaimsAgentService
    {
        private readonly IAgentIdfyService agentIdService;

        public ClaimsAgentService(IAgentIdfyService agentIdService)
        {
            this.agentIdService = agentIdService;
        }
        public async Task<AppiCheckifyResponse> PostDocumentId(string userEmail, string reportName, string locationName, long locationId, long claimId, long docId, string latitude, string longitude, IFormFile Image)
        {
            var locationLongLat = string.IsNullOrWhiteSpace(latitude) || string.IsNullOrWhiteSpace(longitude) ? string.Empty : $"{latitude}/{longitude}";

            var data = new DocumentData
            {
                LocationName = locationName,
                ReportName = reportName,
                Email = userEmail,
                CaseId = claimId,
                Image = Image,
                LocationLatLong = locationLongLat
            };
            var result = await agentIdService.CaptureDocumentId(data);
            return result;
        }

        public async Task<AppiCheckifyResponse> PostAgentId(string userEmail, string reportName, string locationName, long locationId, long claimId, long faceId, string latitude, string longitude, bool isAgent, IFormFile Image)
        {
            var locationLongLat = string.IsNullOrWhiteSpace(latitude) || string.IsNullOrWhiteSpace(longitude) ? string.Empty : $"{latitude}/{longitude}";

            var data = new FaceData
            {
                LocationName = locationName,
                ReportName = reportName,
                Email = userEmail,
                CaseId = claimId,
                Image = Image,
                LocationLatLong = locationLongLat
            };
            if (isAgent)
            {
                var result = await agentIdService.CaptureAgentId(data);
                return result;
            }
            else
            {
                var result = await agentIdService.CaptureFaceId(data);
                return result;
            }

        }
    }
}
