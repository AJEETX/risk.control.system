using Gehtsoft.PDFFlow.Builder;
using Gehtsoft.PDFFlow.Models.Enumerations;
using Gehtsoft.PDFFlow.Utils;
using risk.control.system.Models;
using risk.control.system.Services.Common;

namespace risk.control.system.Services.Report
{
    public interface IPdfGenerateDocumentLocationService
    {
        Task<SectionBuilder> Build(SectionBuilder section, LocationReport loc, bool isClaim = true);
    }
    internal class PdfGenerateDocumentLocationService(IWebHostEnvironment webHostEnvironment, IHttpClientFactory httpClientFactory, IImageConverter imageConverter) : IPdfGenerateDocumentLocationService
    {
        internal static readonly FontBuilder FNT9 = Fonts.Helvetica(9f);
        private readonly IWebHostEnvironment _env = webHostEnvironment;
        private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
        private readonly IImageConverter _imageConverter = imageConverter;

        public async Task<SectionBuilder> Build(SectionBuilder section, LocationReport loc, bool isClaim = true)
        {
            if (loc.DocumentIds != null && loc.DocumentIds.Count != 0)
            {
                section.AddParagraph().AddText("");
                section.AddParagraph().SetLineSpacing(1).AddText("Document ID Reports ").SetFontSize(14).SetBold().SetUnderline();
                var tableBuilder = section.AddTable().SetBorder(Stroke.Solid);
                tableBuilder.AddColumnPercentToTable("Photo type", 10).AddColumnPercentToTable("Photo", 20).AddColumnPercentToTable("Captured Address", 20).AddColumnPercentToTable("Scan Info", 20).AddColumnPercentToTable("Map Image", 25).AddColumnPercentToTable("Valid", 5);
                foreach (var doc in loc.DocumentIds.Where(f => f.Selected && f.ValidationExecuted))
                {
                    var rowBuilder = tableBuilder.AddRow();
                    rowBuilder.AddCell().AddParagraph().AddText(doc.ReportName!).SetFont(FNT9);
                    var pngBytes = _imageConverter.ConvertToPngFromPath(_env, doc.FilePath!);
                    rowBuilder.AddCell().SetVerticalAlignment(VerticalAlignment.Center).SetHorizontalAlignment(HorizontalAlignment.Center).AddParagraph().AddInlineImage(pngBytes).SetWidth(140F);
                    var addressData = $"{doc.LocationAddress}\r\nCaptured Date & Time:{doc.LongLatTime.GetValueOrDefault():dd-MMM-yy hh:mm tt}";
                    rowBuilder.AddCell().AddParagraph(addressData).SetFont(FNT9);
                    string location = isClaim ? "Beneficiary" : "Life-Assured";
                    var locData = $"Indicative Distance from {location} Address:{doc.Distance}\r\nMore Info: {doc.LocationInfo}";
                    rowBuilder.AddCell().AddParagraph(locData).SetFont(FNT9);
                    var mapImage = await _imageConverter.DownloadMapImageAsync(_httpClientFactory, string.Format(doc.LocationMapUrl!, "300", "300"));
                    var cell = rowBuilder.AddCell().SetVerticalAlignment(VerticalAlignment.Center).SetHorizontalAlignment(HorizontalAlignment.Center);
                    var paragraph = cell.AddParagraph();
                    paragraph.AddInlineImage(mapImage).SetWidth(180);
                    string mapUrl = string.Format(doc.LocationMapUrl!, "600", "600");
                    paragraph.AddText("\r\n");
                    paragraph.AddUrlToParagraph(mapUrl, "View Full Map")
                             .SetFont(FNT9)
                             .SetFontColor(Gehtsoft.PDFFlow.Models.Shared.Color.Blue)
                             .SetUnderline();

                    string matchResult = doc.ImageValid == true ? "YES" : "NO";
                    rowBuilder.AddCell().AddParagraph(matchResult).SetFontSize(14).SetBold().SetFontColor(doc.ImageValid == true ? Gehtsoft.PDFFlow.Models.Shared.Color.Green : Gehtsoft.PDFFlow.Models.Shared.Color.Red);
                }
                section.AddParagraph().AddText("");
            }
            return section;
        }
    }
}
