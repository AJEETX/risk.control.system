using Amazon.Rekognition;
using Amazon.Rekognition.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.FeatureManagement;
using risk.control.system.AppConstant;
using risk.control.system.Helpers;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;
using risk.control.system.Services.Agent;
using risk.control.system.Services.Common;

namespace risk.control.system.Seeds
{
    public static class MigrateImagesToAws
    {
        public static async Task MigrateExistingUsersToCollectionAsync(IAmazonApiService _amazonApiService, ApplicationDbContext _context, IBase64FileService base64FileService, IFeatureManager featureManager)
        {
            //Delete the existing face image collection if it exists
            var imageCollection = EnvHelper.Get(CONSTANTS.FaceImageCollection);
            var deletedResponse = await _amazonApiService.DeleteCollectionAsync(imageCollection!);

            //Delete the existing bucket if it exists
            var s3BucketName = EnvHelper.Get(CONSTANTS.S3_BUCKET)!;
            await _amazonApiService.DeleteBucketAsync(s3BucketName);

            var uploadImage2Aws = await featureManager.IsEnabledAsync(FeatureFlags.FACE_MATCH_CHECK);
            if (uploadImage2Aws)
            {
                var ensureCollectionResponse = await _amazonApiService.EnsureCollectionExistsAsync(imageCollection!);

                var portalUsers = _context.Users.AsNoTracking().Where(u => u.AwsFaceId == null && u.ProfilePictureUrl != null).AsAsyncEnumerable();
                await foreach (var user in portalUsers)
                {
                    try
                    {
                        byte[] imageBytes = await base64FileService.GetByteFileAsync(user.ProfilePictureUrl!);
                        var indexRequest = new IndexFacesRequest
                        {
                            CollectionId = imageCollection,
                            Image = new Amazon.Rekognition.Model.Image { Bytes = new MemoryStream(imageBytes) },
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
                            _context.Users.Update(user);
                            await _context.SaveChangesAsync();
                        }
                        Console.WriteLine($"Indexed User {user.Id}: Found {response.FaceRecords.Count} face(s)");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Failed to index User {user.Id}: {ex.Message}");
                    }
                }
            }

        }
    }
}