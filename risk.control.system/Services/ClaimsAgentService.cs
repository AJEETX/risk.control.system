﻿using risk.control.system.Models.ViewModel;

namespace risk.control.system.Services
{
    public interface IClaimsAgentService
    {
        Task<AppiCheckifyResponse> PostAgentId(string userEmail, string reportName, string locationName, long locationId, long claimId, long faceId, string latitude, string longitude,bool isAgent, IFormFile Image);
        //Task<AppiCheckifyResponse> PostFaceId(string userEmail, long locationId, long claimId, long faceId, string latitude, string longitude, byte[]? image = null);
        Task<AppiCheckifyResponse> PostDocumentId(string userEmail, string reportName, string locationName, long locationId, long claimId, long docId, string latitude, string longitude, IFormFile Image);
    }
    public class ClaimsAgentService : IClaimsAgentService
    {
        private readonly IAgentIdService agentIdService;

        public ClaimsAgentService(IAgentIdService agentIdService)
        {
            this.agentIdService = agentIdService;
        }
        public async Task<AppiCheckifyResponse> PostDocumentId(string userEmail, string reportName, string locationName, long locationId, long claimId, long docId, string latitude, string longitude, IFormFile Image)
        {
            var locationLongLat = string.IsNullOrWhiteSpace(latitude) || string.IsNullOrWhiteSpace(longitude) ? string.Empty : $"{latitude}/{longitude}";
            
            var data = new DocumentData
            {
                LocationName = locationName,
                //LocationId = locationId,
                //Id = docId,
                ReportName = reportName,
                Email = userEmail,
                CaseId = claimId,
                Image = Image,
                LocationLatLong = locationLongLat
            };
            var result = await agentIdService.GetDocumentId(data);
            return result;
        }
        //public async Task<AppiCheckifyResponse> PostFaceId(string userEmail, long locationId, long claimId, long faceId, string latitude, string longitude, byte[]? image = null)
        //{
        //    var locationLongLat = string.IsNullOrWhiteSpace(latitude) || string.IsNullOrWhiteSpace(longitude) ? string.Empty : $"{latitude}/{longitude}";
        //    var data = new FaceData
        //    {
        //        LocationId = locationId,
        //        Id = faceId,
        //        Email = userEmail,
        //        CaseId = claimId,
        //        LocationImage = Convert.ToBase64String(image),
        //        LocationLongLat = locationLongLat
        //    };
        //    var result = await agentIdService.GetFaceId(data);
        //    return result;
        //}

        public async Task<AppiCheckifyResponse> PostAgentId(string userEmail, string reportName, string locationName, long locationId, long claimId, long faceId, string latitude, string longitude, bool isAgent, IFormFile Image)
        {
            var locationLongLat = string.IsNullOrWhiteSpace(latitude) || string.IsNullOrWhiteSpace(longitude) ? string.Empty : $"{latitude}/{longitude}";
            
            var data = new FaceData
            {
                LocationName = locationName,
                ReportName = reportName,
                Type = isAgent ? "0":"1",
                //LocationId = locationId,
                //Id = faceId,
                Email = userEmail,
                CaseId = claimId,
                Image = Image,
                LocationLatLong = locationLongLat
            };
            if(isAgent)
            {
                var result = await agentIdService.GetAgentId(data);
                return result;
            }
            else
            {
                var result = await agentIdService.GetFaceId(data);
                return result;
            }
            
        }
    }
}
