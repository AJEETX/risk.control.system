using Amazon.Rekognition;
using Amazon.Rekognition.Model;
using Microsoft.EntityFrameworkCore;
using risk.control.system.AppConstant;
using risk.control.system.Models;
using risk.control.system.Services.Agent;
using risk.control.system.Services.Common;

namespace risk.control.system.Seeds
{
    public static class MigrateImagesToAws
    {
        public static async Task MigrateExistingUsersToCollectionAsync(IAmazonApiService _amazonApiService, ApplicationDbContext _context, IBase64FileService base64FileService)
        {
            await _amazonApiService.EnsureCollectionExistsAsync(CONSTANTS.AgencyUsersImageCollection);
            await foreach (var user in _context.Users.AsNoTracking().Where(u => u.AwsFaceId == null && u.ProfilePictureUrl != null).Select(u => new { u.Id, u.ProfilePictureUrl }).AsAsyncEnumerable())
            {
                try
                {
                    byte[] imageBytes = await base64FileService.GetByteFileAsync(user.ProfilePictureUrl!);
                    var indexRequest = new IndexFacesRequest
                    {
                        CollectionId = CONSTANTS.AgencyUsersImageCollection,
                        Image = new Image { Bytes = new MemoryStream(imageBytes) },
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