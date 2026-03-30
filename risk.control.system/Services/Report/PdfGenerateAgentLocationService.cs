using Gehtsoft.PDFFlow.Builder;
using Gehtsoft.PDFFlow.Models.Enumerations;
using Gehtsoft.PDFFlow.Utils;
using risk.control.system.Helpers;
using risk.control.system.Models;

namespace risk.control.system.Services.Report
{
    public interface IPdfGenerateAgentLocationService
    {
        Task<SectionBuilder> Build(SectionBuilder section, LocationReport loc, bool isClaim = true);
    }

    internal class PdfGenerateAgentLocationService : IPdfGenerateAgentLocationService
    {
        internal static readonly FontBuilder FNT9 = Fonts.Helvetica(9f);
        internal static readonly FontBuilder FNT10 = Fonts.Helvetica(10f);
        internal static readonly FontBuilder FNT12 = Fonts.Helvetica(12f);
        internal static readonly FontBuilder FNT12B = Fonts.Helvetica(12f).SetBold(true);
        internal static readonly FontBuilder FNT20 = Fonts.Helvetica(20f);
        internal static readonly FontBuilder FNT19B = Fonts.Helvetica(19f).SetBold();
        internal static readonly FontBuilder FNT8 = Fonts.Helvetica(8f);
        internal static readonly FontBuilder FNT8_G = Fonts.Helvetica(8f).SetColor(Gehtsoft.PDFFlow.Models.Shared.Color.Gray);
        internal static readonly FontBuilder FNT9B = Fonts.Helvetica(9f).SetBold();
        internal static readonly FontBuilder FNT11B = Fonts.Helvetica(11f).SetBold();
        internal static readonly FontBuilder FNT15 = Fonts.Helvetica(15f);
        internal static readonly FontBuilder FNT16 = Fonts.Helvetica(16f);
        internal static readonly FontBuilder FNT16_R = Fonts.Helvetica(16f).SetColor(Gehtsoft.PDFFlow.Models.Shared.Color.Red);
        internal static readonly FontBuilder FNT16_G = Fonts.Helvetica(16f).SetColor(Gehtsoft.PDFFlow.Models.Shared.Color.Green);
        internal static readonly FontBuilder FNT17 = Fonts.Helvetica(17f);
        internal static readonly FontBuilder FNT18 = Fonts.Helvetica(18f);
        private readonly IWebHostEnvironment webHostEnvironment;
        private readonly IHttpClientFactory httpClientFactory;

        public PdfGenerateAgentLocationService(IWebHostEnvironment webHostEnvironment, IHttpClientFactory httpClientFactory)
        {
            this.webHostEnvironment = webHostEnvironment;
            this.httpClientFactory = httpClientFactory;
        }

        public async Task<SectionBuilder> Build(SectionBuilder section, LocationReport loc, bool isClaim)
        {
            var imagePath = webHostEnvironment.WebRootPath;
            string googlePhotoImagePath = Path.Combine(imagePath, "report", $"google-agent-map-{DateTime.UtcNow.ToString("ddMMMyyyHHmmsss")}.png");
            if (loc.AgentIdReport != null && loc.AgentIdReport.ValidationExecuted)
            {
                var duration = loc.Updated.GetValueOrDefault().Subtract(loc.AgentIdReport.LongLatTime.GetValueOrDefault());
                var durationDisplay = "Time spent :" + (duration.Hours > 0 ? $"{duration.Hours}h " : "") + (duration.Minutes > 0 ? $"{duration.Minutes}m" : "less than a min");
                section.AddParagraph().SetLineSpacing(2).AddText($"{durationDisplay}").SetFontSize(12);
                section.AddParagraph().SetLineSpacing(2).AddText($"Verifying Agent: {loc.AgentEmail}").SetFontSize(12).SetItalic();
                section.AddParagraph().SetLineSpacing(2).AddText($"Capture Date:{loc.AgentIdReport.LongLatTime.GetValueOrDefault().ToString("dd-MMM-yyyy HH:mm")}").SetFontSize(10).SetUnderline();
                var tableBuilder = section.AddTable().SetBorder(Stroke.Solid);
                tableBuilder.AddColumnPercentToTable("Agent Photo", 20).AddColumnPercentToTable("Captured Address", 20).AddColumnPercentToTable("Address Info", 20).AddColumnPercentToTable("Map Image", 35).AddColumnPercentToTable("Match", 5);
                var rowBuilder = tableBuilder.AddRow();
                var pngBytes = ImageConverterToPng.ConvertToPngFromPath(webHostEnvironment, loc.AgentIdReport.FilePath!);
                rowBuilder.AddCell().AddParagraph().AddInlineImage(pngBytes).SetWidth(100);
                var addressData = $"DateTime:{loc.AgentIdReport.LongLatTime.GetValueOrDefault().ToString("dd-MMM-yyyy HH:mm")} \r\n {loc.AgentIdReport.LocationAddress}";
                rowBuilder.AddCell().AddParagraph(addressData).SetFont(FNT9);
                string location = isClaim ? "Beneficiary " : "Life-Assured ";
                var locData = $"Indicative Distance from {location} Address :{loc.AgentIdReport.Distance}\r\n {loc.AgentIdReport.LocationInfo}";
                rowBuilder.AddCell().AddParagraph(locData).SetFont(FNT9);
                string downloadedImagePath = await DownloadMapImageAsync(string.Format(loc.AgentIdReport.LocationMapUrl!, "300", "300"), googlePhotoImagePath);
                rowBuilder.AddCell().AddParagraph().AddInlineImage(downloadedImagePath).SetWidth(150);
                string matchResult = loc.AgentIdReport.ImageValid == true ? "✓" : "✗";
                rowBuilder.AddCell().AddParagraph(matchResult).SetFontSize(14).SetBold().SetFontColor(loc.AgentIdReport.ImageValid == true ? Gehtsoft.PDFFlow.Models.Shared.Color.Green : Gehtsoft.PDFFlow.Models.Shared.Color.Red);
            }
            return section;
        }

        private async Task<string> DownloadMapImageAsync(string url, string outputFilePath)
        {
            var client = httpClientFactory.CreateClient();
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