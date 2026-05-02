using Gehtsoft.PDFFlow.Builder;
using Gehtsoft.PDFFlow.Models.Enumerations;
using risk.control.system.Models;
using risk.control.system.Services.Common;

namespace risk.control.system.Services.Report
{
    public interface IPdfGenerateFaceLocationService
    {
        Task<SectionBuilder> Build(SectionBuilder section, LocationReport loc, bool isClaim = true);
    }
    internal class PdfGenerateFaceLocationService(IWebHostEnvironment env, IHttpClientFactory httpClientFactory, IImageConverter imageConverter) : IPdfGenerateFaceLocationService
    {
        private readonly IWebHostEnvironment _env = env;
        private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
        private readonly IImageConverter _imageConverter = imageConverter;

        public async Task<SectionBuilder> Build(SectionBuilder section, LocationReport loc, bool isClaim = true)
        {
            if (loc.FaceIds != null && loc.FaceIds.Count != 0 && loc.FaceIds.Any(f => f.ValidationExecuted))
            {
                section.AddParagraph().AddText("");
                section.AddParagraph().SetLineSpacing(1).AddText("Face ID Reports").SetFontSize(14).SetBold().SetUnderline();
                var tableBuilder = section.AddTable().SetBorder(Stroke.Solid);
                tableBuilder.AddColumnPercentToTable("Photo type", 10).AddColumnPercentToTable("Photo", 20).AddColumnPercentToTable("Captured Address Info", 20).AddColumnPercentToTable("Location Info", 20).AddColumnPercentToTable("Map Image", 25).AddColumnPercentToTable("Match", 5);
                foreach (var face in loc.FaceIds.Where(f => f.Selected && f.ValidationExecuted))
                {
                    var rowBuilder = tableBuilder.AddRow();
                    rowBuilder.AddCell().AddParagraph().AddText(face.ReportName!).SetFontSize(9F);
                    var pngBytes = _imageConverter.ConvertToPngFromPath(_env, face.FilePath!);
                    rowBuilder.AddCell().SetVerticalAlignment(VerticalAlignment.Center).SetHorizontalAlignment(HorizontalAlignment.Center).AddParagraph().AddInlineImage(pngBytes).SetWidth(140F);
                    string location = isClaim ? "Beneficiary" : "Life-Assured";
                    var addressData = $"{face.LocationAddress}\r\n\r\nIndicative Distance from {location} Address:{face.Distance}\r\n\r\nCaptured Date & Time:{face.LongLatTime.GetValueOrDefault().ToLocalTime():dd-MMM-yy hh:mm tt}";
                    rowBuilder.AddCell().AddParagraph(addressData).SetFontSize(9F);
                    var locData = $"{face.LocationInfo}";
                    rowBuilder.AddCell().AddParagraph(locData).SetFontSize(9F);
                    var mapImage = await _imageConverter.DownloadMapImageAsync(_httpClientFactory, string.Format(face.LocationMapUrl!, "300", "300"));
                    var cell = rowBuilder.AddCell().SetVerticalAlignment(VerticalAlignment.Center).SetHorizontalAlignment(HorizontalAlignment.Center);
                    var paragraph = cell.AddParagraph();
                    paragraph.AddInlineImage(mapImage).SetWidth(180);
                    string mapUrl = string.Format(face.LocationMapUrl!, "600", "600");
                    paragraph.AddText("\r\n");
                    paragraph.AddUrlToParagraph(mapUrl, "View Full Map").SetFontSize(9F)
                             .SetFontColor(Gehtsoft.PDFFlow.Models.Shared.Color.Blue)
                             .SetUnderline();
                    string matchResult = loc.AgentIdReport!.ImageValid == true ? Path.Combine(_env.WebRootPath, "img", "yes.png") : Path.Combine(_env.WebRootPath, "img", "cancel.png");
                    rowBuilder.AddCell().SetVerticalAlignment(VerticalAlignment.Center).SetHorizontalAlignment(HorizontalAlignment.Center).AddParagraph().AddInlineImage(matchResult).SetWidth(30F);
                }
            }
            return section;
        }
    }
}
