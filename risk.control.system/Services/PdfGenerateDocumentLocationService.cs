using Gehtsoft.PDFFlow.Builder;
using Gehtsoft.PDFFlow.Models.Enumerations;
using risk.control.system.Models;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp;
using Gehtsoft.PDFFlow.Utils;
using risk.control.system.Helpers;

namespace risk.control.system.Services
{
    public interface IPdfGenerateDocumentLocationService
    {
        Task<SectionBuilder> Build(SectionBuilder section, LocationTemplate loc);

    }
    public class PdfGenerateDocumentLocationService : IPdfGenerateDocumentLocationService
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
        public PdfGenerateDocumentLocationService(IWebHostEnvironment webHostEnvironment)
        {
            this.webHostEnvironment = webHostEnvironment;
        }
        public async Task<SectionBuilder> Build(SectionBuilder section, LocationTemplate loc)
        {
            var imagePath = webHostEnvironment.WebRootPath;

            string googlePhotoImagePath = Path.Combine(imagePath, "report", $"google-face-map-{DateTime.Now.ToString("ddMMMyyyHHmmsss")}.png");
            //// =================== DOCUMENT IDs ====================
            if (loc.DocumentIds?.Any() == true)
            {
                section.AddParagraph()
                       .SetLineSpacing(1)
                       .AddText($"Document ID Reports ")
                       .SetFontSize(14)
                       .SetBold()
                       .SetUnderline();

                // Create a simple table without any styling to check the issue
                var tableBuilder = section.AddTable()
                                          .SetBorder(Stroke.Solid);

                tableBuilder
                     .AddColumnPercentToTable("Photo type", 10)
                    .AddColumnPercentToTable("Photo", 20)
                    .AddColumnPercentToTable("Captured Address", 20)
                    .AddColumnPercentToTable("Scan Info", 20)
                    .AddColumnPercentToTable("Map Image", 25)
                    .AddColumnPercentToTable("Valid", 5);

                foreach (var face in loc.DocumentIds.Where(f => f.Selected))
                {
                    var rowBuilder = tableBuilder.AddRow();
                    rowBuilder.AddCell().AddParagraph().AddText(face.ReportName);

                    if (face.IdImage != null)
                    {
                        try
                        {
                            var pngBytes = ImageConverterToPng.ConvertToPng(face.IdImage, face.IdImageExtension);
                            rowBuilder.AddCell().AddParagraph().AddInlineImage(pngBytes);
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
                    rowBuilder.AddCell().AddParagraph().AddText(face.IdImageLocationAddress);

                    var locData = $"DateTime:{face.Updated.GetValueOrDefault()} || {face.IdImageData}";
                    rowBuilder.AddCell().AddParagraph().AddText(locData);
                    if (face.IdImageLocationUrl != null)
                    {
                        try
                        {
                            // Download the image
                            string downloadedImagePath = await DownloadMapImageAsync(string.Format(face.IdImageLocationUrl,"300","300"), googlePhotoImagePath);
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
                    string matchResult = face.IdImageValid == true ? "✓" : "✗";
                    rowBuilder.AddCell().AddParagraph(matchResult).SetFontSize(12).SetBold()
                                .SetFontColor(face.IdImageValid == true ? Gehtsoft.PDFFlow.Models.Shared.Color.Green : Gehtsoft.PDFFlow.Models.Shared.Color.Red);
                }

                section.AddParagraph().AddText("..");
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
}
