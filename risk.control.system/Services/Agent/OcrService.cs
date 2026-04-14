using System.Text.RegularExpressions;
using Amazon.Textract;
using Amazon.Textract.Model;
using risk.control.system.Helpers;
using risk.control.system.Models;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Processing;

namespace risk.control.system.Services.Agent;

public interface IOcrService
{
    Task<(string ocrText, string panNumber, byte[] imageBytes)> ExtractTextDataAsync(DocumentIdReport doc, byte[] bytes);

    Task<byte[]> MaskPanNumber(byte[] imageBytes, BoundingBox box);
}

public class OcrService(ILogger<OcrService> logger, IAmazonTextract amazonTextract, IProcessImageService processImageService) : IOcrService
{
    private readonly ILogger _logger = logger;
    private readonly IAmazonTextract _amazonTextract = amazonTextract;
    private readonly IProcessImageService _processImageService = processImageService;

    // Updated return type to include the PAN string
    public async Task<(string ocrText, string panNumber, byte[] imageBytes)> ExtractTextDataAsync(DocumentIdReport doc, byte[] bytes)
    {
        try
        {
            var request = new DetectDocumentTextRequest
            {
                Document = new Document { Bytes = new MemoryStream(bytes) }
            };

            var response = await _amazonTextract.DetectDocumentTextAsync(request);

            var lineTexts = response.Blocks.Where(b => b.BlockType == BlockType.LINE).Select(b => b.Text);
            var ocrText = string.Join(Environment.NewLine, lineTexts);
            doc.ValidationExecuted = true;

            var isPanCard = doc.ReportName == DocumentIdReportType.PAN.GetEnumDisplayName();
            if (isPanCard)
            {
                var panBlock = response.Blocks.FirstOrDefault(b => b.BlockType == BlockType.LINE && Regex.IsMatch(b.Text, @"[A-Z]{5}[0-9]{4}[A-Z]{1}"));
                string panNumber = panBlock?.Text ?? "Not Found";
                if (panBlock == null)
                {
                    var compressedDocumentImage = _processImageService.CompressImage(bytes);
                    await File.WriteAllBytesAsync(doc.FilePath!, compressedDocumentImage);
                    doc.ImageValid = true;
                    doc.LocationInfo = ocrText;
                    return (ocrText, panNumber, bytes);
                }

                // 3. Masking logic remains the same
                var boundingBox = panBlock.Geometry.BoundingBox;
                var maskedImageBytes = await MaskPanNumber(bytes, boundingBox);

                var compressedPanImage = _processImageService.CompressImage(maskedImageBytes);
                await File.WriteAllBytesAsync(doc.FilePath!, compressedPanImage);
                doc.ImageValid = true;

                doc.LocationInfo = ocrText.Replace(panNumber, "XXXXXXXXXXX");
                return (ocrText, panNumber, maskedImageBytes);
            }
            else
            {
                var compressed = _processImageService.CompressImage(bytes);
                await File.WriteAllBytesAsync(doc.FilePath!, compressed);
                doc.ImageValid = false;
                doc.LocationInfo = ocrText;
                return (ocrText, "...", bytes);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while extracting text data.");
            throw;
        }
    }

    public async Task<byte[]> MaskPanNumber(byte[] imageBytes, BoundingBox box)
    {
        using (Image image = Image.Load(imageBytes))
        {
            int width = image.Width;
            int height = image.Height;

            // Convert Textract relative coordinates to actual pixels
            var rectWidth = box.Width * width;
            var rectHeight = box.Height * height;
            var rectX = box.Left * width;
            var rectY = box.Top * height;

            // Draw a black box over the PAN number
            image.Mutate(ctx => ctx.Fill(Color.Black, new RectangleF(rectX!.Value, rectY!.Value, rectWidth!.Value, rectHeight!.Value)));

            using (var ms = new MemoryStream())
            {
                await image.SaveAsJpegAsync(ms); // Or your preferred format
                return ms.ToArray();
            }
        }
    }
}