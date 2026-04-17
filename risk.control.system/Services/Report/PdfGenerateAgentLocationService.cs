using Gehtsoft.PDFFlow.Builder;
using Gehtsoft.PDFFlow.Models.Enumerations;
using Gehtsoft.PDFFlow.Utils;
using risk.control.system.Models;
using risk.control.system.Services.Common;

namespace risk.control.system.Services.Report
{
    public interface IPdfGenerateAgentLocationService
    {
        Task<SectionBuilder> Build(SectionBuilder section, LocationReport loc, bool isClaim = true);
    }

    internal class PdfGenerateAgentLocationService(IWebHostEnvironment env, IHttpClientFactory httpClientFactory, IImageConverter imageConverter) : IPdfGenerateAgentLocationService
    {
        internal static readonly FontBuilder FNT9 = Fonts.Helvetica(9f);
        private readonly IWebHostEnvironment _env = env;
        private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
        private readonly IImageConverter _imageConverter = imageConverter;

        public async Task<SectionBuilder> Build(SectionBuilder section, LocationReport loc, bool isClaim)
        {
            if (loc.AgentIdReport?.ValidationExecuted == true)
            {
                var duration = loc.Updated.GetValueOrDefault().Subtract(loc.AgentIdReport.LongLatTime.GetValueOrDefault());
                var durationDisplay = "Time spent:" + (duration.Hours > 0 ? $"{duration.Hours}h " : "") + (duration.Minutes > 0 ? $"{duration.Minutes}m" : "less than a min");
                section.AddParagraph().AddText($"{durationDisplay}").SetFontSize(12);
                section.AddParagraph().AddText($"Verifying Agent: {loc.AgentEmail}").SetFontSize(12).SetItalic();
                var tableBuilder = section.AddTable().SetBorder(Stroke.Solid);
                tableBuilder.AddColumnPercentToTable("Agent Photo", 30).AddColumnPercentToTable("Captured Address", 20).AddColumnPercentToTable("Address Info", 20).AddColumnPercentToTable("Map Image", 25).AddColumnPercentToTable("Match", 5);
                var rowBuilder = tableBuilder.AddRow();
                byte[] pngBytes = _imageConverter.ConvertToPngFromPath(_env, loc.AgentIdReport.FilePath!);
                rowBuilder.AddCell().SetVerticalAlignment(VerticalAlignment.Center).SetHorizontalAlignment(HorizontalAlignment.Center).AddParagraph().AddInlineImage(pngBytes).SetWidth(160F);
                var addressData = $"{loc.AgentIdReport.LocationAddress}\r\nCaptured Date & Time:{loc.AgentIdReport.LongLatTime.GetValueOrDefault():dd-MMM-yy hh:mm tt}";
                rowBuilder.AddCell().AddParagraph(addressData).SetFont(FNT9);
                string location = isClaim ? "Beneficiary" : "Life-Assured";
                var locData = $"Indicative Distance from {location} Address:{loc.AgentIdReport.Distance}\r\nMore Info:{loc.AgentIdReport.LocationInfo}";
                rowBuilder.AddCell().AddParagraph(locData).SetFont(FNT9);
                var mapImage = await _imageConverter.DownloadMapImageAsync(_httpClientFactory, string.Format(loc.AgentIdReport.LocationMapUrl!, "400", "400"));
                var cell = rowBuilder.AddCell().SetVerticalAlignment(VerticalAlignment.Center).SetHorizontalAlignment(HorizontalAlignment.Center);
                var paragraph = cell.AddParagraph();
                paragraph.AddInlineImage(mapImage).SetWidth(180);
                string mapUrl = string.Format(loc.AgentIdReport.LocationMapUrl!, "600", "600");
                paragraph.AddText("\r\n");
                paragraph.AddUrlToParagraph(mapUrl, "View Full Map")
                         .SetFont(FNT9)
                         .SetFontColor(Gehtsoft.PDFFlow.Models.Shared.Color.Blue)
                         .SetUnderline();
                string matchResult = loc.AgentIdReport.ImageValid == true ? Path.Combine(_env.WebRootPath, "img", "yes.png") : Path.Combine(_env.WebRootPath, "img", "cancel.png");
                rowBuilder.AddCell().SetVerticalAlignment(VerticalAlignment.Center).SetHorizontalAlignment(HorizontalAlignment.Center).AddParagraph().AddInlineImage(matchResult).SetWidth(30F);
            }
            return section;
        }
    }
}