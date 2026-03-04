using Amazon.Rekognition;
using Amazon.Rekognition.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.FeatureManagement;
using risk.control.system.AppConstant;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;
using risk.control.system.Services.Agent;
using Image = Amazon.Rekognition.Model.Image;

namespace risk.control.system.Services.Common
{
    public interface IUserFaceImageCheckService
    {
        Task SetImageToAws(string userEmail);

        Task<bool> CheckFaceImageAsync(IFormFile imageFile);
    }

    internal class UserFaceImageCheckService : IUserFaceImageCheckService
    {
        private readonly ApplicationDbContext _context;
        private readonly IAmazonApiService _amazonApiService;
        private readonly IBase64FileService base64FileService;
        private readonly IFeatureManager _featureManager;

        public UserFaceImageCheckService(
            ApplicationDbContext context,
            IAmazonApiService amazonApiService,
            IBase64FileService base64FileService,
            IFeatureManager featureManager)
        {
            _context = context;
            _amazonApiService = amazonApiService;
            this.base64FileService = base64FileService;
            _featureManager = featureManager;
        }

        public async Task<bool> CheckFaceImageAsync(IFormFile imageFile)
        {
            if (!await _featureManager.IsEnabledAsync(FeatureFlags.ENABLE_AGENCY_USER_FACE_MATCH))
            {
                return true; // If the feature is disabled, skip face matching and return true
            }

            using var memoryStream = new MemoryStream();
            await imageFile.CopyToAsync(memoryStream);

            var searchRequest = new SearchFacesByImageRequest
            {
                CollectionId = CONSTANTS.AgencyUsersImageCollection,
                Image = new Image { Bytes = memoryStream },
                MaxFaces = 1,
                FaceMatchThreshold = 90F
            };

            var response = await _amazonApiService.SearchFacesByImageAsync(searchRequest);
            if (response.FaceMatches.Count > 0)
            {
                // Get the UserId we stored during indexing
                string userId = response.FaceMatches[0].Face.ExternalImageId;

                // Fetch the actual user object from your DB
                var matchingUser = await _context.Users.FindAsync(Guid.Parse(userId));
            }

            var match = response.FaceMatches.Count > 0;
            return match;
        }

        public async Task SetImageToAws(string userEmail)
        {
            if (await _featureManager.IsEnabledAsync(FeatureFlags.ENABLE_AGENCY_USER_FACE_MATCH))
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == userEmail);
                byte[] imageBytes = await base64FileService.GetByteFileAsync(user.ProfilePictureUrl);

                var indexRequest = new IndexFacesRequest
                {
                    CollectionId = CONSTANTS.AgencyUsersImageCollection,
                    Image = new Image { Bytes = new MemoryStream(imageBytes) },
                    // VERY IMPORTANT: Store the DB Primary Key here
                    ExternalImageId = user.Id.ToString(),
                    MaxFaces = 1,
                    QualityFilter = QualityFilter.AUTO
                };

                var response = await _amazonApiService.IndexFacesAsync(indexRequest);
                var faceId = response.FaceRecords.FirstOrDefault()?.Face.FaceId;
                if (faceId != null)
                {
                    var userToUpdate = new ApplicationUser { Id = user.Id };
                    _context.Users.Attach(userToUpdate);

                    userToUpdate.AwsFaceId = faceId;
                    userToUpdate.FaceIndexedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                }
            }
        }
    }
}