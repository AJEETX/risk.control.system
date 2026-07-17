using Amazon.Rekognition;
using Amazon.Rekognition.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.FeatureManagement;
using risk.control.system.AppConstant;
using risk.control.system.Helpers;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;
using risk.control.system.Services.Agent;
using Image = Amazon.Rekognition.Model.Image;

namespace risk.control.system.Services.Common
{
    public interface IAwsFaceImageCheckService
    {
        Task SetUserImageToAws(string userEmail);
        Task SetCaseImagesToAws(long caseId);
        Task<bool> CheckFaceImageExistAsync(IFormFile imageFile);
        Task<bool> CheckUploadFaceImageExistAsync(byte[] image);
        Task<FaceMatchResult> HasExactlyOneFace(IFormFile file);
        Task<FaceMatchResult> HasExactlyOneFace(byte[] imageBytes);
    }

    internal class AwsFaceImageCheckService(
        IDbContextFactory<ApplicationDbContext> contextFactory,
        IAmazonApiService amazonApiService,
        IBase64FileService base64FileService,
        IFeatureManager featureManager) : IAwsFaceImageCheckService
    {
        private readonly IDbContextFactory<ApplicationDbContext> _contextFactory = contextFactory;
        private readonly IAmazonApiService _amazonApiService = amazonApiService;
        private readonly IBase64FileService _base64FileService = base64FileService;
        private readonly IFeatureManager _featureManager = featureManager;

        public async Task<bool> CheckUploadFaceImageExistAsync(byte[] image)
        {
            if (!await _featureManager.IsEnabledAsync(FeatureFlags.FACE_MATCH_CHECK))
            {
                return false;
            }
            await using var memoryStream = new MemoryStream(image);

            var searchRequest = new SearchFacesByImageRequest
            {
                CollectionId = EnvHelper.Get(CONSTANTS.FaceImageCollection),
                Image = new Image { Bytes = memoryStream },
                MaxFaces = 1,
                FaceMatchThreshold = 90F
            };

            var response = await _amazonApiService.SearchFacesByImageAsync(searchRequest);
            //if (response.FaceMatches.Count > 0)
            //{
            //    string userId = response.FaceMatches[0].Face.ExternalImageId;

            //    // Create a local DbContext instance for this scoped operation
            //    await using var context = await _contextFactory.CreateDbContextAsync();
            //    var matchingUser = await context.Users.FindAsync(Guid.Parse(userId));
            //}

            var match = response.FaceMatches.Count > 0;
            return match;
        }

        public async Task<bool> CheckFaceImageExistAsync(IFormFile imageFile)
        {
            if (!await _featureManager.IsEnabledAsync(FeatureFlags.FACE_MATCH_CHECK))
            {
                return false;
            }

            await using var memoryStream = new MemoryStream();
            await imageFile.CopyToAsync(memoryStream);

            var searchRequest = new SearchFacesByImageRequest
            {
                CollectionId = EnvHelper.Get(CONSTANTS.FaceImageCollection),
                Image = new Image { Bytes = memoryStream },
                MaxFaces = 1,
                FaceMatchThreshold = 90F
            };

            var response = await _amazonApiService.SearchFacesByImageAsync(searchRequest);
            var match = response.FaceMatches.Count > 0;
            return match;
        }

        public async Task<FaceMatchResult> HasExactlyOneFace(IFormFile file)
        {
            if (!await _featureManager.IsEnabledAsync(FeatureFlags.FACE_MATCH_CHECK))
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
            if (!await _featureManager.IsEnabledAsync(FeatureFlags.FACE_MATCH_CHECK))
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

        public async Task SetUserImageToAws(string userEmail)
        {
            if (await _featureManager.IsEnabledAsync(FeatureFlags.FACE_MATCH_CHECK))
            {
                // Create a local DbContext instance
                await using var context = await _contextFactory.CreateDbContextAsync();

                var user = await context.Users.FirstOrDefaultAsync(u => u.Email == userEmail);
                if (user == null) return;

                byte[] imageBytes = await _base64FileService.GetByteFileAsync(user.ProfilePictureUrl!);

                var indexRequest = new IndexFacesRequest
                {
                    CollectionId = EnvHelper.Get(CONSTANTS.FaceImageCollection),
                    Image = new Image { Bytes = new MemoryStream(imageBytes) },
                    ExternalImageId = user.Id.ToString(),
                    MaxFaces = 1,
                    QualityFilter = QualityFilter.AUTO
                };

                var response = await _amazonApiService.IndexFacesAsync(indexRequest);
                var faceId = response.FaceRecords.FirstOrDefault()?.Face.FaceId;
                if (faceId != null)
                {
                    user.AwsFaceId = faceId;
                    user.FaceIndexedAt = DateTime.UtcNow;
                    await context.SaveChangesAsync();
                }
            }
        }

        public async Task SetCaseImagesToAws(long caseId)
        {
            if (await _featureManager.IsEnabledAsync(FeatureFlags.FACE_MATCH_CHECK))
            {
                await using var context = await _contextFactory.CreateDbContextAsync();

                await SetCaseCustomerImageToAws(caseId, context);
                await SetCaseBeneficiaryImageToAws(caseId, context);
                await context.SaveChangesAsync();
            }
        }

        private async Task SetCaseCustomerImageToAws(long caseId, ApplicationDbContext context)
        {
            var customer = await context.CustomerDetail.FirstOrDefaultAsync(i => i.InvestigationTaskId == caseId);
            if (customer == null) return;

            var customerImageBytes = await _base64FileService.GetByteFileAsync(customer.ImagePath!);
            var indexRequest = new IndexFacesRequest
            {
                CollectionId = EnvHelper.Get(CONSTANTS.FaceImageCollection),
                Image = new Image { Bytes = new MemoryStream(customerImageBytes) },
                ExternalImageId = customer.CustomerDetailId.ToString(),
                MaxFaces = 1,
                QualityFilter = QualityFilter.AUTO
            };

            var response = await _amazonApiService.IndexFacesAsync(indexRequest);
            var faceId = response.FaceRecords.FirstOrDefault()?.Face.FaceId;
            if (faceId != null)
            {
                customer.AwsFaceId = faceId;
                customer.FaceIndexedAt = DateTime.UtcNow;
            }
        }

        private async Task SetCaseBeneficiaryImageToAws(long caseId, ApplicationDbContext context)
        {
            var beneficiary = await context.BeneficiaryDetail.FirstOrDefaultAsync(i => i.InvestigationTaskId == caseId);
            if (beneficiary == null) return;

            var beneficiaryImageBytes = await _base64FileService.GetByteFileAsync(beneficiary.ImagePath!);
            var indexRequest = new IndexFacesRequest
            {
                CollectionId = EnvHelper.Get(CONSTANTS.FaceImageCollection),
                Image = new Image { Bytes = new MemoryStream(beneficiaryImageBytes) },
                ExternalImageId = beneficiary.BeneficiaryDetailId.ToString(),
                MaxFaces = 1,
                QualityFilter = QualityFilter.AUTO
            };

            var response = await _amazonApiService.IndexFacesAsync(indexRequest);
            var faceId = response.FaceRecords.FirstOrDefault()?.Face.FaceId;
            if (faceId != null)
            {
                beneficiary.AwsFaceId = faceId;
                beneficiary.FaceIndexedAt = DateTime.UtcNow;
            }
        }
    }
}