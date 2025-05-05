using Gehtsoft.PDFFlow.Builder;
using Gehtsoft.PDFFlow.Models.Enumerations;
using risk.control.system.Models;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp;
using Gehtsoft.PDFFlow.Utils;

namespace risk.control.system.Services
{
    public interface IPdfGenerateAgentLocationService
    {
        Task<SectionBuilder> Build(SectionBuilder section,LocationTemplate loc);
    }
    public class PdfGenerateAgentLocationService : IPdfGenerateAgentLocationService
    {
        internal static readonly FontBuilder FNT9 = Fonts.Helvetica(9f);
        internal static readonly FontBuilder FNT10 = Fonts.Helvetica(10f);
        internal static readonly FontBuilder FNT12 = Fonts.Helvetica(12f);
        internal static readonly FontBuilder FNT12B = Fonts.Helvetica(12f).SetBold(true);
        internal static readonly FontBuilder FNT20 = Fonts.Helvetica(20f);
        internal static readonly FontBuilder FNT19B = Fonts.Helvetica(19f).SetBold();

        internal static readonly FontBuilder FNT8 = Fonts.Helvetica(8f);

        internal static readonly FontBuilder FNT8_G =
            Fonts.Helvetica(8f).SetColor(Gehtsoft.PDFFlow.Models.Shared.Color.Gray);

        internal static readonly FontBuilder FNT9B =
            Fonts.Helvetica(9f).SetBold();

        internal static readonly FontBuilder FNT11B =
            Fonts.Helvetica(11f).SetBold();

        internal static readonly FontBuilder FNT15 = Fonts.Helvetica(15f);
        internal static readonly FontBuilder FNT16 = Fonts.Helvetica(16f);

        internal static readonly FontBuilder FNT16_R =
            Fonts.Helvetica(16f).SetColor(Gehtsoft.PDFFlow.Models.Shared.Color.Red);
        internal static readonly FontBuilder FNT16_G =
            Fonts.Helvetica(16f).SetColor(Gehtsoft.PDFFlow.Models.Shared.Color.Green);
        internal static readonly FontBuilder FNT17 = Fonts.Helvetica(17f);
        internal static readonly FontBuilder FNT18 = Fonts.Helvetica(18f);
        private readonly IWebHostEnvironment webHostEnvironment;
        private static HttpClient client = new HttpClient();
        public PdfGenerateAgentLocationService(IWebHostEnvironment webHostEnvironment)
        {
            this.webHostEnvironment = webHostEnvironment;
        }
        public async Task<SectionBuilder> Build(SectionBuilder section,  LocationTemplate loc)
        {
            var imagePath = webHostEnvironment.WebRootPath;
            string googlePhotoImagePath = Path.Combine(imagePath, "report", $"google-agent-map-{DateTime.Now.ToString("ddMMMyyyHHmmsss")}.png");
            // =================== AGENT ID REPORT ====================
            if (loc.AgentIdReport != null)
            {
                // Section title
                section.AddParagraph()
                       .SetLineSpacing(1)
                       .AddText($"Agent ID  : {loc.AgentIdReport.Updated.GetValueOrDefault().ToString("dd-MMM-yyyy")}")
                       .SetFontSize(14)
                       .SetBold()
                       .SetUnderline();

                // Build the table
                var tableBuilder = section.AddTable()
                                          .SetBorder(Stroke.Solid);
                // Add columns
                tableBuilder
                    .AddColumnPercentToTable("Agent Photo", 20)
                    .AddColumnPercentToTable("Captured Address", 20)
                    .AddColumnPercentToTable("Address Info", 20)
                    .AddColumnPercentToTable("Map Image", 35)
                    .AddColumnPercentToTable("Status", 5);

                var rowBuilder = tableBuilder.AddRow();
                if (loc.AgentIdReport.IdImage != null)
                {
                    try
                    {
                        var pngBytes = ImageConverter.ConvertToPng(loc.AgentIdReport.IdImage);
                        rowBuilder.AddCell().AddParagraph().AddInlineImage(pngBytes)
                      .SetWidth(100)
                      .SetHeight(100);
                    }
                    catch (Exception ex)
                    {
                        rowBuilder.AddCell().AddParagraph().AddText("Invalid image");
                        Console.WriteLine("Image conversion error: " + ex.Message);
                    }
                }
                else
                {
                    rowBuilder.AddCell().AddParagraph().AddText("No Image");
                }
                rowBuilder.AddCell().AddParagraph(loc.AgentIdReport.IdImageLocationAddress).SetFont(FNT9);
                rowBuilder.AddCell().AddParagraph(loc.AgentIdReport.IdImageData).SetFont(FNT9);
                
                if (loc.AgentIdReport.IdImageLocationUrl != null)
                {
                    try
                    {
                        // Download the image
                        string downloadedImagePath = await DownloadMapImageAsync(loc.AgentIdReport.IdImageLocationUrl, googlePhotoImagePath);
                        rowBuilder.AddCell()
                                  .AddParagraph()
                                  .AddInlineImage(downloadedImagePath)
                                  .SetWidth(150)
                                  .SetHeight(150);
                    }
                    catch
                    {
                        rowBuilder.AddCell().AddParagraph("Invalid Map").SetFontSize(9);
                    }
                }
                else
                {
                    rowBuilder.AddCell().AddParagraph("No Map").SetFontSize(9);
                }
                // Match icon using Unicode
                string matchResult = loc.AgentIdReport.IdImageValid == true ? "✓" : "✗";
                rowBuilder.AddCell().AddParagraph(matchResult).SetFontSize(12).SetBold()
                            .SetFontColor(loc.AgentIdReport.IdImageValid == true ? Gehtsoft.PDFFlow.Models.Shared.Color.Green : Gehtsoft.PDFFlow.Models.Shared.Color.Red);

            }
            return section;
        }

        static async Task<string> DownloadMapImageAsync(string url, string outputFilePath)
        {
            var response = await client.GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                var imageBytes = await response.Content.ReadAsByteArrayAsync();
                await File.WriteAllBytesAsync(outputFilePath, imageBytes);
                return outputFilePath;
            }
            else
            {
                throw new Exception($"Failed to download map image. Status: {response.StatusCode}");
            }
        }
        
    }
    public static class ImageConverter
    {
        public static byte[] ConvertToPng(byte[] imageBytes)
        {
            if (imageBytes == null || imageBytes.Length == 0)
                throw new ArgumentException("Input image data is null or empty.", nameof(imageBytes));

            try
            {
                using var inputStream = new MemoryStream(imageBytes);
                using var image = Image.Load(inputStream); // Auto-detects format
                using var outputStream = new MemoryStream();

                var pngEncoder = new PngEncoder
                {
                    CompressionLevel = PngCompressionLevel.DefaultCompression,
                    ColorType = PngColorType.Rgb
                };

                image.Save(outputStream, pngEncoder);
                return outputStream.ToArray();
            }
            catch (SixLabors.ImageSharp.UnknownImageFormatException)
            {
                throw new InvalidOperationException("The provided byte array is not a supported image format.");
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to convert image to PNG format.", ex);
            }
        }
    }
}
