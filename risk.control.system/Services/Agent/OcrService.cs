using Amazon.Textract;
using Amazon.Textract.Model;

namespace risk.control.system.Services.Agent;

public interface IOcrService
{
    Task<string> ExtractTextDataAsync(byte[] bytes);
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

    public async Task<string> ExtractTextDataAsync(byte[] bytes)
    {
        try
        {
            var request = new DetectDocumentTextRequest
            {
                Document = new Document { Bytes = new MemoryStream(bytes) }
            };

            var response = await amazonTextract.DetectDocumentTextAsync(request);
            var lineTexts = response.Blocks.Where(b => b.BlockType == BlockType.LINE).Select(b => b.Text);
            var ocrText = string.Join(Environment.NewLine, lineTexts);
            return ocrText;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while extracting text data.");
            throw;
        }
    }
}