using Gehtsoft.PDFFlow.Builder;
using Gehtsoft.PDFFlow.Models.Enumerations;
using Gehtsoft.PDFFlow.Utils;
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
        internal static readonly FontBuilder FNT9 = Fonts.Helvetica(9f);
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
                tableBuilder.AddColumnPercentToTable("Photo type", 10).AddColumnPercentToTable("Photo", 20).AddColumnPercentToTable("Captured Address", 20).AddColumnPercentToTable("Location Info", 20).AddColumnPercentToTable("Map Image", 25).AddColumnPercentToTable("Match", 5);
                foreach (var face in loc.FaceIds.Where(f => f.Selected && f.ValidationExecuted))
                {
                    var rowBuilder = tableBuilder.AddRow();
                    rowBuilder.AddCell().AddParagraph().AddText(face.ReportName!).SetFont(FNT9);
                    var pngBytes = _imageConverter.ConvertToPngFromPath(_env, face.FilePath!);
                    rowBuilder.AddCell().SetVerticalAlignment(VerticalAlignment.Center).SetHorizontalAlignment(HorizontalAlignment.Center).AddParagraph().AddInlineImage(pngBytes).SetWidth(140F);
                    var addressData = $"{face.LocationAddress}\r\nCaptured Date & Time:{face.LongLatTime.GetValueOrDefault():dd-MMM-yy hh:mm tt}";
                    rowBuilder.AddCell().AddParagraph(addressData).SetFont(FNT9);
                    string location = isClaim ? "Beneficiary" : "Life-Assured";
                    var locData = $"Indicative Distance from {location} Address:{face.Distance}\r\nMore Info: {face.LocationInfo}";
                    rowBuilder.AddCell().AddParagraph(locData).SetFont(FNT9);
                    var mapImage = await _imageConverter.DownloadMapImageAsync(_httpClientFactory, string.Format(face.LocationMapUrl!, "300", "300"));
                    var cell = rowBuilder.AddCell().SetVerticalAlignment(VerticalAlignment.Center).SetHorizontalAlignment(HorizontalAlignment.Center);
                    var paragraph = cell.AddParagraph();
                    paragraph.AddInlineImage(mapImage).SetWidth(180);
                    string mapUrl = string.Format(face.LocationMapUrl!, "600", "600");
                    paragraph.AddText("\r\n");
                    paragraph.AddUrlToParagraph(mapUrl, "View Full Map")
                             .SetFont(FNT9)
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
