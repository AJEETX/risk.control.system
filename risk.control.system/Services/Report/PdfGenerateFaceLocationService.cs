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
                    var faceImagePngBytes = _imageConverter.ConvertToPngFromPath(_env, face.FilePath!);
                    var agentPhotoCell = rowBuilder.AddCell().SetVerticalAlignment(VerticalAlignment.Center).SetHorizontalAlignment(HorizontalAlignment.Center);

                    // 2. Add the Image Paragraph
                    var imagePara = agentPhotoCell.AddParagraph();
                    imagePara.AddInlineImage(faceImagePngBytes).SetWidth(120F).SetHeight(120F);

                    if (face.FaceResult!.Faces.Count == 0)
                    {
                        var noFacePara = agentPhotoCell.AddParagraph();
                        noFacePara.AddText("No face detected").SetFontSize(9F).SetBold();
                    }
                    else if (face.FaceResult!.Faces.Count > 1)
                    {
                        var noFacePara = agentPhotoCell.AddParagraph();
                        noFacePara.AddText("Multiple faces detected").SetFontSize(9F).SetBold();
                    }
                    else
                    {
                        var agentFaceResult = face.FaceResult!.Faces.FirstOrDefault();
                        // 3. Add Line 1 of text (e.g., Bold Label)
                        var line1 = agentPhotoCell.AddParagraph();
                        line1.AddText($"Age Range: {face.FaceResult!.Faces.FirstOrDefault()?.AgeRange ?? "N/A"}").SetFontSize(9F).SetBold();

                        // 4. Add Line 2 of text (e.g., Subtext)
                        var line2 = agentPhotoCell.AddParagraph();
                        line2.AddText($"Gender: {face.FaceResult!.Faces.FirstOrDefault()?.Gender ?? "N/A"}").SetFontSize(9F).SetBold();

                        var line3 = agentPhotoCell.AddParagraph();
                        line3.AddText($"Emotion: {face.FaceResult!.Faces.FirstOrDefault()?.PrimaryEmotion ?? "N/A"}").SetFontSize(9F).SetBold();

                        var line4 = agentPhotoCell.AddParagraph();
                        var smileValue = face.FaceResult!.Faces.FirstOrDefault()!.IsSmiling ? "Yes" : "No";
                        line4.AddText($"Smiling: {smileValue ?? "N/A"}").SetFontSize(9F).SetBold();

                        var line5 = agentPhotoCell.AddParagraph();
                        var glassesValue = face.FaceResult!.Faces.FirstOrDefault()!.IsWearingGlasses ? "Yes" : "No";
                        line5.AddText($"Wearing Glasses: {glassesValue ?? "N/A"}").SetFontSize(9F).SetBold();

                        var line6 = agentPhotoCell.AddParagraph();
                        var beardValue = face.FaceResult!.Faces.FirstOrDefault()!.HasBeard ? "Yes" : "No";
                        line6.AddText($"Bearded: {beardValue ?? "N/A"}").SetFontSize(9F).SetBold();
                    }

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
                    string imgFileName = face!.ImageValid == true ? "yes.png" : "cancel.png";
                    string matchImagePath = Path.Combine(_env.WebRootPath, "img", imgFileName);

                    // 2. Create the separate text string for the match result
                    string matchText = face.ImageValid == true
                        ? $"YES\r\n({face.Similarity}% Match)"
                        : $"NO\r\n({face.Similarity}% No Match)";

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
            }
            return section;
        }
    }
}
