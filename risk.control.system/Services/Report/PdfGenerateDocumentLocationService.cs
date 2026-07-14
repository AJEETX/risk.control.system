using Gehtsoft.PDFFlow.Builder;
using Gehtsoft.PDFFlow.Models.Enumerations;
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
                tableBuilder.AddColumnPercentToTable("Photo type", 10).AddColumnPercentToTable("Photo", 20).AddColumnPercentToTable("Captured Address Info", 20).AddColumnPercentToTable("Scan Info", 20).AddColumnPercentToTable("Map Image", 25).AddColumnPercentToTable("Valid", 5);
                foreach (var doc in loc.DocumentIds.Where(f => f.Selected && f.ValidationExecuted))
                {
                    var rowBuilder = tableBuilder.AddRow();
                    rowBuilder.AddCell().AddParagraph().AddText(doc.ReportName!).SetFontSize(9F);
                    var pngBytes = _imageConverter.ConvertToPngFromPath(_env, doc.FilePath!);
                    rowBuilder.AddCell().SetVerticalAlignment(VerticalAlignment.Center).SetHorizontalAlignment(HorizontalAlignment.Center).AddParagraph().AddInlineImage(pngBytes).SetWidth(140F);
                    string location = isClaim ? "Beneficiary" : "Life-Assured";
                    var addressData = $"{doc.LocationAddress}\r\n\r\nIndicative Distance from {location} Address:{doc.Distance}\r\n\r\nCaptured Date & Time:{doc.LongLatTime.GetValueOrDefault().ToLocalTime():dd-MMM-yy hh:mm tt}";
                    rowBuilder.AddCell().AddParagraph(addressData).SetFontSize(9F);
                    var locData = $"{doc.LocationInfo}";
                    rowBuilder.AddCell().AddParagraph(locData).SetFontSize(9F);
                    var mapImage = await _imageConverter.DownloadMapImageAsync(_httpClientFactory, string.Format(doc.LocationMapUrl!, "300", "300"));
                    var cell = rowBuilder.AddCell().SetVerticalAlignment(VerticalAlignment.Center).SetHorizontalAlignment(HorizontalAlignment.Center);
                    var paragraph = cell.AddParagraph();
                    paragraph.AddInlineImage(mapImage).SetWidth(180);
                    string mapUrl = string.Format(doc.LocationMapUrl!, "600", "600");
                    paragraph.AddText("\r\n");
                    paragraph.AddUrlToParagraph(mapUrl, "View Full Map").SetFontSize(9F)
                             .SetFontColor(Gehtsoft.PDFFlow.Models.Shared.Color.Blue)
                             .SetUnderline();

                    string imgFileName = doc!.ImageValid == true ? "yes.png" : "cancel.png";
                    string matchImagePath = Path.Combine(_env.WebRootPath, "img", imgFileName);

                    // 2. Create the separate text string for the match result
                    string matchText = doc.ImageValid == true ? "YES" : "NO";

                    // 3. Build the cell and paragraph
                    var matchCell = rowBuilder.AddCell()
                        .SetVerticalAlignment(VerticalAlignment.Center)
                        .SetHorizontalAlignment(HorizontalAlignment.Center);

                    var matchParagraph = matchCell.AddParagraph();

                    // 4. Add the image safely if it exists
                    if (System.IO.File.Exists(matchImagePath))
                    {
                        matchParagraph.AddInlineImage(matchImagePath).SetWidth(25F);
                        matchParagraph.AddText("\r\n"); // Line break to place text below the icon
                    }

                    // 5. Add the text result below the image
                    matchParagraph.AddText(matchText)
                                  .SetFontSize(8F)
                                  .SetBold();
                }
                section.AddParagraph().AddText("");
            }
            return section;
        }
    }
}
