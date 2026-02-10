using Amazon.Textract;
using Amazon.Textract.Model;

namespace risk.control.system.Services;

public interface IOcrService
{
    Task<List<Block>> ExtractTextDataAsync(byte[] bytes);
}

public class OcrService : IOcrService
{
    private readonly ILogger _logger;
    private readonly IAmazonTextract amazonTextract;

    public OcrService(ILogger<OcrService> logger, IAmazonTextract amazonTextract)
    {
        _logger = logger;
        this.amazonTextract = amazonTextract;
    }

    public async Task<List<Block>> ExtractTextDataAsync(byte[] bytes)
    {
        try
        {
            var request = new DetectDocumentTextRequest
            {
                Document = new Document { Bytes = new MemoryStream(bytes) }
            };

            var response = await amazonTextract.DetectDocumentTextAsync(request);
            return response.Blocks;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while extracting text data.");
            throw;
        }
    }
}