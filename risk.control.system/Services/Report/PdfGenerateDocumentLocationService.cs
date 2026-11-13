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
    internal class PdfGenerateDocumentLocationService(IWebHostEnvironment env, IImageConverter imageConverter) : IPdfGenerateDocumentLocationService
    {
        private readonly IWebHostEnvironment _env = env;
        private readonly IImageConverter _imageConverter = imageConverter;

        public async Task<SectionBuilder> Build(SectionBuilder section, LocationReport loc, bool isClaim = true)
        {
            if (loc.DocumentIds != null && loc.DocumentIds.Count != 0)
            {
                section.AddParagraph().AddText("");
                section.AddParagraph().SetLineSpacing(1).AddText("Document ID Reports").SetFontSize(14).SetBold().SetUnderline();
                section.AddParagraph().AddText("");

                foreach (var doc in loc.DocumentIds.Where(f => f.Selected && f.ValidationExecuted))
                {
                    // ==========================================
                    // TABLE 1: Main Metadata & Visuals (Full Width)
                    // ==========================================
                    var tableBuilder = section.AddTable().SetBorder(Stroke.Solid);

                    tableBuilder.AddColumnPercentToTable("Photo type", 12)
                                .AddColumnPercentToTable("Photo", 22)
                                .AddColumnPercentToTable("Captured Address Info", 24)
                                .AddColumnPercentToTable("Map Image", 32)
                                .AddColumnPercentToTable("Valid", 10);

                    var rowBuilder = tableBuilder.AddRow();

                    // 1. Photo Type
                    rowBuilder.AddCell().AddParagraph(doc.ReportName ?? "").SetFontSize(9F).SetBold();

                    // 2. Photo
                    var firstFile = doc.DocumentFiles?.OrderBy(o => o.PageNumber).FirstOrDefault();
                    var cellPhoto = rowBuilder.AddCell().SetVerticalAlignment(VerticalAlignment.Center).SetHorizontalAlignment(HorizontalAlignment.Center);
                    if (firstFile != null && !string.IsNullOrEmpty(firstFile.FilePath))
                    {
                        var pngBytes = _imageConverter.ConvertToPngFromPath(firstFile.FilePath);
                        if (pngBytes != null)
                        {
                            cellPhoto.AddParagraph().AddInlineImage(pngBytes).SetWidth(130F);
                        }
                    }

                    // 3. Captured Address Info
                    string location = isClaim ? "Beneficiary" : "Life-Assured";
                    var addressData = $"Captured Address:\r\n{doc.LocationAddress}\r\n\r\n" +
                                      $"Indicative Distance from {location}:\r\n{doc.Distance}\r\n\r\n" +
                                      $"Captured Date & Time:\r\n{doc.LongLatTime.GetValueOrDefault().ToLocalTime():dd-MMM-yy hh:mm tt}";
                    rowBuilder.AddCell().AddParagraph(addressData).SetFontSize(8.5F);

                    // 4. Map Image
                    var mapCell = rowBuilder.AddCell().SetVerticalAlignment(VerticalAlignment.Center).SetHorizontalAlignment(HorizontalAlignment.Center);
                    var mapParagraph = mapCell.AddParagraph();
                    if (!string.IsNullOrEmpty(doc.LocationMapUrl))
                    {
                        var mapImage = await _imageConverter.DownloadMapImageAsync(string.Format(doc.LocationMapUrl, "300", "300"));
                        if (mapImage != null)
                        {
                            mapParagraph.AddInlineImage(mapImage).SetWidth(160);
                            mapParagraph.AddText("\r\n");
                        }
                        string mapUrl = string.Format(doc.LocationMapUrl, "600", "600");
                        mapParagraph.AddUrlToParagraph(mapUrl, "View Full Map").SetFontSize(8.5F)
                                 .SetFontColor(Gehtsoft.PDFFlow.Models.Shared.Color.Blue)
                                 .SetUnderline();
                    }

                    // 5. Valid Status
                    bool isValid = doc.ImageValid.GetValueOrDefault(false);
                    string imgFileName = isValid ? "yes.png" : "cancel.png";
                    string matchImagePath = Path.Combine(_env.WebRootPath, "img", imgFileName);
                    string matchText = isValid ? "YES" : "NO";

                    var matchCell = rowBuilder.AddCell()
                        .SetVerticalAlignment(VerticalAlignment.Center)
                        .SetHorizontalAlignment(HorizontalAlignment.Center);
                    var matchParagraph = matchCell.AddParagraph();

                    if (System.IO.File.Exists(matchImagePath))
                    {
                        matchParagraph.AddInlineImage(matchImagePath).SetWidth(20F);
                        matchParagraph.AddText("\r\n");
                    }
                    matchParagraph.AddText(matchText).SetFontSize(8F).SetBold();

                    // ==========================================
                    // PAGE-WISE SCANNED IMAGE & OCR FLOW
                    // ==========================================
                    if (doc.DocumentFiles != null && doc.DocumentFiles.Any())
                    {
                        section.AddParagraph().AddText("");
                        section.AddParagraph().AddText($"Scanned Content — {doc.ReportName}").SetFontSize(10F).SetBold();

                        foreach (var file in doc.DocumentFiles.OrderBy(o => o.PageNumber))
                        {
                            // Page Header Line
                            section.AddParagraph().AddText($"Page {file.PageNumber} (Verified Scan)").SetFontSize(8.5F).SetBold();

                            // Render Scanned Page Image Above OCR Text
                            if (!string.IsNullOrEmpty(file.FilePath))
                            {
                                var pagePngBytes = _imageConverter.ConvertToPngFromPath(file.FilePath);
                                if (pagePngBytes != null)
                                {
                                    var imgParagraph = section.AddParagraph();
                                    imgParagraph.SetAlignment(HorizontalAlignment.Center);
                                    imgParagraph.AddInlineImage(pagePngBytes).SetWidth(200F); // Adjustable width for page scan view
                                    section.AddParagraph().AddText("");
                                }
                            }

                            // Render OCR Text Below Scanned Image
                            if (!string.IsNullOrWhiteSpace(file.OcrText))
                            {
                                var textParagraph = section.AddParagraph();
                                textParagraph.SetAlignment(HorizontalAlignment.Center);
                                var lines = file.OcrText.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);

                                foreach (var rawLine in lines)
                                {
                                    string line = rawLine.Trim();
                                    bool isLikelyHeader = (line.EndsWith(":") || line.EndsWith("?") ||
                                                           System.Text.RegularExpressions.Regex.IsMatch(line, @"^(\([A-Za-z0-9]+\)|\d+\.)") ||
                                                           (line.Length < 45 && line == line.ToUpper() && line.Length > 3));

                                    if (isLikelyHeader)
                                    {
                                        textParagraph.AddText(line + "\r\n").SetFontSize(7.5F).SetBold();
                                    }
                                    else
                                    {
                                        textParagraph.AddText(line + "\r\n").SetFontSize(7F);
                                    }
                                }
                            }

                            section.AddParagraph().AddText(""); // Spacing between pages
                        }
                    }

                    // Spacing between document blocks
                    section.AddParagraph().AddText("");
                    section.AddParagraph().AddText("");
                }
            }
            return section;
        }
    }
}
