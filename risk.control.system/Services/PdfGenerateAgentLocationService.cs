using Gehtsoft.PDFFlow.Builder;
using Gehtsoft.PDFFlow.Models.Enumerations;
using Gehtsoft.PDFFlow.Utils;

using risk.control.system.Helpers;
using risk.control.system.Models;

namespace risk.control.system.Services
{
    public interface IPdfGenerateAgentLocationService
    {
        Task<SectionBuilder> Build(SectionBuilder section, LocationReport loc, bool isClaim = true);
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
        public async Task<SectionBuilder> Build(SectionBuilder section, LocationReport loc, bool isClaim)
        {
            var imagePath = webHostEnvironment.WebRootPath;
            string googlePhotoImagePath = Path.Combine(imagePath, "report", $"google-agent-map-{DateTime.Now.ToString("ddMMMyyyHHmmsss")}.png");
            // =================== AGENT ID REPORT ====================
            if (loc.AgentIdReport != null && loc.AgentIdReport.ValidationExecuted)
            {
                var duration = loc.Updated.GetValueOrDefault().Subtract(loc.AgentIdReport.LongLatTime.GetValueOrDefault());
                var durationDisplay = "Time spent :" + (duration.Hours > 0 ? $"{duration.Hours}h " : "") + (duration.Minutes > 0 ? $"{duration.Minutes}m" : "less than a min");

                section.AddParagraph()
                .SetLineSpacing(2)
                   .AddText($"{durationDisplay}")
                   .SetFontSize(12);

                section.AddParagraph()
                       .SetLineSpacing(2)
                       .AddText($"Verifying Agent: {loc.AgentEmail}")
                       .SetFontSize(12)
                       .SetItalic();
                // Section title
                section.AddParagraph()
                       .SetLineSpacing(2)
                       .AddText($"Capture Date:{loc.AgentIdReport.LongLatTime.GetValueOrDefault().ToString("dd-MMM-yyyy HH:mm")}")
                       .SetFontSize(10)
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
                    .AddColumnPercentToTable("Match", 5);

                var rowBuilder = tableBuilder.AddRow();
                if (loc.AgentIdReport.Image != null)
                {
                    try
                    {
                        var pngBytes = ImageConverterToPng.ConvertToPng(loc.AgentIdReport.Image, loc.AgentIdReport.ImageExtension);
                        rowBuilder.AddCell().AddParagraph().AddInlineImage(pngBytes)
                      .SetWidth(100);
                    }
                    catch (Exception ex)
                    {
                        rowBuilder.AddCell().AddParagraph().AddText("Invalid image").SetFont(FNT9);
                        Console.WriteLine("Image conversion error: " + ex.Message);
                    }
                }
                else
                {
                    rowBuilder.AddCell().AddParagraph().AddText("No Image").SetFont(FNT9);
                }
                var addressData = $"DateTime:{loc.AgentIdReport.LongLatTime.GetValueOrDefault().ToString("dd-MMM-yyyy HH:mm")} \r\n {loc.AgentIdReport.LocationAddress}";
                rowBuilder.AddCell().AddParagraph(addressData).SetFont(FNT9);
                string location = isClaim ? "Beneficiary " : "Life-Assured ";
                var locData = $"Indicative Distance from {location} Address :{loc.AgentIdReport.Distance}\r\n {loc.AgentIdReport.LocationInfo}";
                rowBuilder.AddCell().AddParagraph(locData).SetFont(FNT9);

                if (loc.AgentIdReport.LocationMapUrl != null)
                {
                    try
                    {
                        // Download the image
                        string downloadedImagePath = await DownloadMapImageAsync(string.Format(loc.AgentIdReport.LocationMapUrl, "300", "300"), googlePhotoImagePath);
                        rowBuilder.AddCell()
                                  .AddParagraph()
                                  .AddInlineImage(downloadedImagePath)
                                  .SetWidth(150);
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
                string matchResult = loc.AgentIdReport.ImageValid == true ? "✓" : "✗";
                rowBuilder.AddCell().AddParagraph(matchResult).SetFontSize(14).SetBold()
                            .SetFontColor(loc.AgentIdReport.ImageValid == true ? Gehtsoft.PDFFlow.Models.Shared.Color.Green : Gehtsoft.PDFFlow.Models.Shared.Color.Red);

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
