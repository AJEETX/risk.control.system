using Amazon.Rekognition;
using Amazon.Rekognition.Model;
using risk.control.system.AppConstant;
using risk.control.system.Services.Agent;

namespace risk.control.system.Services.Agentic
{
    public interface IAgenticService
    {
        Task<(bool, string)> FaceExistsAsync(IFormFile image);
    }
    internal class AgenticService(IAmazonApiService amazonApiService) : IAgenticService
    {
        private readonly IAmazonApiService _amazonApiService = amazonApiService;
        public async Task<(bool, string)> FaceExistsAsync(IFormFile image)
        {
            await _amazonApiService.EnsureCollectionExistsAsync(CONSTANTS.FaceImageCollection);

            var memoryStream = new MemoryStream();

            await image.CopyToAsync(memoryStream);

            var searchRequest = new SearchFacesByImageRequest
            {
                CollectionId = CONSTANTS.FaceImageCollection,
                Image = new Image { Bytes = memoryStream },
                MaxFaces = 1,
                FaceMatchThreshold = 90F
            };

            var response = await _amazonApiService.SearchFacesByImageAsync(searchRequest);
            if (response.FaceMatches.Count > 0)
            {
                return (true, "Face match found.");
            }
            var indexRequest = new IndexFacesRequest
            {
                CollectionId = CONSTANTS.FaceImageCollection,
                Image = new Image { Bytes = memoryStream },
                ExternalImageId = Guid.NewGuid().ToString(),
                MaxFaces = 1,
                QualityFilter = QualityFilter.AUTO
            };
            var faceAddResponse = await _amazonApiService.IndexFacesAsync(indexRequest);
            var faceId = faceAddResponse.FaceRecords.FirstOrDefault()?.Face.FaceId;
            if (faceId != null)
            {
                return (false, "Face added to collection.");
            }
            return (true, "Error occurred while processing the image.");
        }
    }
}
