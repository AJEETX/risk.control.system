using risk.control.system.Models.ViewModel;

namespace risk.control.system.Services
{
    public interface IClaimsAgentService
    {
        Task<AppiCheckifyResponse> PostAgentId(string userEmail, long claimId, string latitude, string longitude, byte[]? image = null);
        Task<AppiCheckifyResponse> PostFaceId(string userEmail, long claimId, string latitude, string longitude, byte[]? image = null);
        //Task<AppiCheckifyResponse> PostAudio(string userEmail, string claimId, string latitude, string longitude, string filename, byte[]? image = null);
        //Task<AppiCheckifyResponse> PostVideo(string userEmail, string claimId, string latitude, string longitude, string filename, byte[]? image = null);
        Task<AppiCheckifyResponse> PostDocumentId(string userEmail, long claimId, string latitude, string longitude, byte[]? image = null);
        //Task<AppiCheckifyResponse> PostPassportId(string userEmail, string claimId, string latitude, string longitude, byte[]? image = null);
    }
    public class ClaimsAgentService : IClaimsAgentService
    {
        private readonly IICheckifyService checkifyService;
        private readonly IAgentIdService agentIdService;

        public ClaimsAgentService(IICheckifyService checkifyService, IAgentIdService agentIdService)
        {
            this.checkifyService = checkifyService;
            this.agentIdService = agentIdService;
        }
        //public async Task<AppiCheckifyResponse> PostAudio(string userEmail, string claimId, string latitude, string longitude, string filename, byte[]? image = null)
        //{
        //    var locationLongLat = string.IsNullOrWhiteSpace(latitude) || string.IsNullOrWhiteSpace(longitude) ? string.Empty : $"{latitude}/{longitude}";

        //    var data = new AudioData
        //    {
        //        Email = userEmail,
        //        ClaimId = claimId,
        //        Mediabytes = image,
        //        LongLat = locationLongLat,
        //        Name = filename
        //    };
        //    var result = await checkifyService.GetAudio(data);
        //    return result;
        //}


        //public async Task<AppiCheckifyResponse> PostVideo(string userEmail, string claimId, string latitude, string longitude, string filename, byte[]? image = null)
        //{
        //    var locationLongLat = string.IsNullOrWhiteSpace(latitude) || string.IsNullOrWhiteSpace(longitude) ? string.Empty : $"{latitude}/{longitude}";

        //    var data = new VideoData
        //    {
        //        Email = userEmail,
        //        ClaimId = claimId,
        //        Mediabytes = image,
        //        LongLat = locationLongLat,
        //        Name = filename
        //    };
        //    var result = await checkifyService.GetVideo(data);
        //    return result;
        //}
        public async Task<AppiCheckifyResponse> PostDocumentId(string userEmail, long claimId, string latitude, string longitude, byte[]? image = null)
        {
            var locationLongLat = string.IsNullOrWhiteSpace(latitude) || string.IsNullOrWhiteSpace(longitude) ? string.Empty : $"{latitude}/{longitude}";

            var data = new DocumentData
            {
                Email = userEmail,
                ClaimId = claimId,
                OcrImage = Convert.ToBase64String(image),
                OcrLongLat = locationLongLat
            };
            var result = await agentIdService.GetDocumentId(data);
            return result;
        }

        //public async Task<AppiCheckifyResponse> PostPassportId(string userEmail, string claimId, string latitude, string longitude, byte[]? image = null)
        //{
        //    var locationLongLat = string.IsNullOrWhiteSpace(latitude) || string.IsNullOrWhiteSpace(longitude) ? string.Empty : $"{latitude}/{longitude}";

        //    var data = new DocumentData
        //    {
        //        Email = userEmail,
        //        ClaimId = claimId,
        //        OcrImage = Convert.ToBase64String(image),
        //        OcrLongLat = locationLongLat
        //    };
        //    var result = await checkifyService.GetPassportId(data);
        //    return result;
        //}

        public async Task<AppiCheckifyResponse> PostFaceId(string userEmail, long claimId, string latitude, string longitude, byte[]? image = null)
        {
            var locationLongLat = string.IsNullOrWhiteSpace(latitude) || string.IsNullOrWhiteSpace(longitude) ? string.Empty : $"{latitude}/{longitude}";
            var data = new FaceData
            {
                Email = userEmail,
                ClaimId = claimId,
                LocationImage = Convert.ToBase64String(image),
                LocationLongLat = locationLongLat
            };
            var result = await agentIdService.GetFaceId(data);
            return result;
        }

        public async Task<AppiCheckifyResponse> PostAgentId(string userEmail, long claimId, string latitude, string longitude, byte[]? image = null)
        {
            var locationLongLat = string.IsNullOrWhiteSpace(latitude) || string.IsNullOrWhiteSpace(longitude) ? string.Empty : $"{latitude}/{longitude}";
            var data = new FaceData
            {
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
