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

        Task<bool> CheckFaceImageExistAsync(IFormFile imageFile);
        Task<bool> CheckUploadFaceImageExistAsync(byte[] image);
        Task<FaceMatchResult> HasExactlyOneFace(IFormFile file);
        Task<FaceMatchResult> HasExactlyOneFace(byte[] imageBytes);
    }

    internal class UserFaceImageCheckService(
        ApplicationDbContext context,
        IAmazonApiService amazonApiService,
        IBase64FileService base64FileService,
        IFeatureManager featureManager) : IUserFaceImageCheckService
    {
        private readonly ApplicationDbContext _context = context;
        private readonly IAmazonApiService _amazonApiService = amazonApiService;
        private readonly IBase64FileService _base64FileService = base64FileService;
        private readonly IFeatureManager _featureManager = featureManager;

        public async Task<bool> CheckUploadFaceImageExistAsync(byte[] image)
        {
            if (!await _featureManager.IsEnabledAsync(FeatureFlags.ENABLE_SINGLE_FACE_MATCH_CHECK))
            {
                return false; // If the feature is disabled, skip face matching and return false indicating there is no matching face that already exist
            }
            await using var memoryStream = new MemoryStream(image);

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
                string userId = response.FaceMatches[0].Face.ExternalImageId;
                var matchingUser = await _context.Users.FindAsync(Guid.Parse(userId));
            }

            var match = response.FaceMatches.Count > 0;
            return match;
        }
        public async Task<bool> CheckFaceImageExistAsync(IFormFile imageFile)
        {
            if (!await _featureManager.IsEnabledAsync(FeatureFlags.ENABLE_SINGLE_FACE_MATCH_CHECK))
            {
                return false; // If the feature is disabled, skip face matching and return false indicating there is no matching face that already exist
            }

            await using var memoryStream = new MemoryStream();
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
                string userId = response.FaceMatches[0].Face.ExternalImageId;
                var matchingUser = await _context.Users.FindAsync(Guid.Parse(userId));
            }

            var match = response.FaceMatches.Count > 0;
            return match;
        }
        public async Task<FaceMatchResult> HasExactlyOneFace(IFormFile file)
        {
            if (!await _featureManager.IsEnabledAsync(FeatureFlags.ENABLE_SINGLE_FACE_MATCH_CHECK))
            {
                return new FaceMatchResult { IsValid = true, Message = "Single face validated." };
            }
            await using var ms = new MemoryStream();
            await file.CopyToAsync(ms);

            var request = new DetectFacesRequest
            {
                Image = new Image { Bytes = ms },
                Attributes = new List<string> { "ALL" }
            };

            var response = await _amazonApiService.ValidateSingleFace(request);

            // Filter for high-confidence detections only
            var highConfidenceFaces = response.FaceDetails
                .Where(f => f.Confidence > 95f)
                .ToList();

            if (highConfidenceFaces.Count == 0)
            {
                return new FaceMatchResult { IsValid = false, Message = "No face detected." };
            }

            if (highConfidenceFaces.Count > 1)
            {
                return new FaceMatchResult { IsValid = false, Message = "Multiple faces detected. Please upload a photo with only one person." };
            }

            return new FaceMatchResult { IsValid = true, Message = "Single face validated." };

        }
        public async Task<FaceMatchResult> HasExactlyOneFace(byte[] imageBytes)
        {
            if (!await _featureManager.IsEnabledAsync(FeatureFlags.ENABLE_SINGLE_FACE_MATCH_CHECK))
            {
                return new FaceMatchResult { IsValid = true, Message = "Single face validated." };
            }
            await using var stream = new MemoryStream(imageBytes);
            var request = new DetectFacesRequest
            {
                Image = new Image { Bytes = stream },
                Attributes = new List<string> { "DEFAULT" }
            };

            var response = await _amazonApiService.ValidateSingleFace(request);

            // Filter for high-confidence detections only
            var highConfidenceFaces = response.FaceDetails
                .Where(f => f.Confidence > 95f)
                .ToList();

            if (highConfidenceFaces.Count == 0)
            {
                return new FaceMatchResult { IsValid = false, Message = "No face detected." };
            }

            if (highConfidenceFaces.Count > 1)
            {
                return new FaceMatchResult { IsValid = false, Message = "Multiple faces detected. Please upload a photo with only one person." };
            }

            return new FaceMatchResult { IsValid = true, Message = "Single face validated." };
        }
        public async Task SetImageToAws(string userEmail)
        {
            if (await _featureManager.IsEnabledAsync(FeatureFlags.ENABLE_SINGLE_FACE_MATCH_CHECK))
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == userEmail);
                byte[] imageBytes = await _base64FileService.GetByteFileAsync(user!.ProfilePictureUrl!);

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