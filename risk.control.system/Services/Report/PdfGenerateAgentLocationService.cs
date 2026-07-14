using Gehtsoft.PDFFlow.Builder;
using Gehtsoft.PDFFlow.Models.Enumerations;
using Microsoft.EntityFrameworkCore;
using risk.control.system.Models;
using risk.control.system.Services.Common;

namespace risk.control.system.Services.Report
{
    public interface IPdfGenerateAgentLocationService
    {
        Task<SectionBuilder> Build(SectionBuilder section, LocationReport loc, bool isClaim = true);
    }

    internal class PdfGenerateAgentLocationService(IWebHostEnvironment env, IHttpClientFactory httpClientFactory, IImageConverter imageConverter, ApplicationDbContext context) : IPdfGenerateAgentLocationService
    {
        private readonly IWebHostEnvironment _env = env;
        private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
        private readonly IImageConverter _imageConverter = imageConverter;
        private readonly ApplicationDbContext _context = context;

        public async Task<SectionBuilder> Build(SectionBuilder section, LocationReport loc, bool isClaim)
        {
            if (loc.AgentIdReport?.ValidationExecuted == true)
            {
                var duration = loc.Updated.GetValueOrDefault().Subtract(loc.AgentIdReport.LongLatTime.GetValueOrDefault());
                var durationDisplay = "Time spent:" + (duration.Hours > 0 ? $"{duration.Hours}hr " : "") + (duration.Minutes > 0 ? $"{duration.Minutes}min" : "less than a min");
                section.AddParagraph().AddText($"{durationDisplay}").SetFontSize(12);
                var agentPara = section.AddParagraph().SetFontSize(12);
                agentPara.AddText("Verifying Agent: ");
                var agent = await _context.Users.FirstOrDefaultAsync(u => u.Email == loc.AgentEmail);
                var agentName = $"{agent!.FirstName} {agent.LastName}";
                agentPara.AddText($"{agentName ?? "N/A"}").SetFontColor(Gehtsoft.PDFFlow.Models.Shared.Color.Blue);
                var tableBuilder = section.AddTable().SetBorder(Stroke.Solid);
                tableBuilder.AddColumnPercentToTable("Agent Photo", 30).AddColumnPercentToTable("Captured Address Info", 20).AddColumnPercentToTable("Address Info", 20).AddColumnPercentToTable("Map Image", 25).AddColumnPercentToTable("Match", 5);
                var rowBuilder = tableBuilder.AddRow();
                byte[] agentImagePngBytes = _imageConverter.ConvertToPngFromPath(_env, loc.AgentIdReport.FilePath!);

                var agentPhotoCell = rowBuilder.AddCell().SetVerticalAlignment(VerticalAlignment.Center).SetHorizontalAlignment(HorizontalAlignment.Center);

                // 2. Add the Image Paragraph
                var imagePara = agentPhotoCell.AddParagraph();
                imagePara.AddInlineImage(agentImagePngBytes).SetWidth(120F).SetHeight(120F);
                if (loc.AgentIdReport.FaceResult?.Faces.Count == 0)
                {
                    var noFacePara = agentPhotoCell.AddParagraph();
                    noFacePara.AddText("No face detected").SetFontSize(9F).SetBold();
                }

                if (loc.AgentIdReport.FaceResult?.Faces.Count > 1)
                {
                    var noFacePara = agentPhotoCell.AddParagraph();
                    noFacePara.AddText("Multiple faces detected").SetFontSize(9F).SetBold();
                }
                else
                {
                    var agentFaceResult = loc.AgentIdReport.FaceResult!.Faces.FirstOrDefault();
                    // 3. Add Line 1 of text (e.g., Bold Label)
                    var line1 = agentPhotoCell.AddParagraph();
                    line1.AddText($"Age Range: {agentFaceResult?.AgeRange ?? "N/A"}").SetFontSize(9F).SetBold();

                    // 4. Add Line 2 of text (e.g., Subtext)
                    var line2 = agentPhotoCell.AddParagraph();
                    line2.AddText($"Gender: {agentFaceResult?.Gender ?? "N/A"}").SetFontSize(9F).SetBold();

                    var line3 = agentPhotoCell.AddParagraph();
                    line3.AddText($"Emotion: {agentFaceResult?.PrimaryEmotion ?? "N/A"}").SetFontSize(9F).SetBold();

                    var line4 = agentPhotoCell.AddParagraph();
                    var smileValue = agentFaceResult!.IsSmiling ? "Yes" : "No";
                    line4.AddText($"Smiling: {smileValue ?? "N/A"}").SetFontSize(9F).SetBold();

                    var line5 = agentPhotoCell.AddParagraph();
                    var glassesValue = agentFaceResult!.IsWearingGlasses ? "Yes" : "No";
                    line5.AddText($"Wearing Glasses: {glassesValue ?? "N/A"}").SetFontSize(9F).SetBold();

                    var line6 = agentPhotoCell.AddParagraph();
                    var beardValue = agentFaceResult!.HasBeard ? "Yes" : "No";
                    line6.AddText($"Bearded: {beardValue ?? "N/A"}").SetFontSize(9F).SetBold();
                }
                string location = isClaim ? "Beneficiary" : "Life-Assured";
                var addressData = $"{loc.AgentIdReport.LocationAddress}\r\n\r\nIndicative Distance from {location} Address:{loc.AgentIdReport.Distance}\r\n\r\nCaptured Date & Time:{loc.AgentIdReport.LongLatTime.GetValueOrDefault().ToLocalTime():dd-MMM-yy hh:mm tt}";
                rowBuilder.AddCell().AddParagraph(addressData).SetFontSize(9F);
                var locData = $"{loc.AgentIdReport.LocationInfo}";
                rowBuilder.AddCell().AddParagraph(locData).SetFontSize(9F);
                var mapImage = await _imageConverter.DownloadMapImageAsync(_httpClientFactory, string.Format(loc.AgentIdReport.LocationMapUrl!, "400", "400"));
                var cell = rowBuilder.AddCell().SetVerticalAlignment(VerticalAlignment.Center).SetHorizontalAlignment(HorizontalAlignment.Center);
                var paragraph = cell.AddParagraph();
                paragraph.AddInlineImage(mapImage).SetWidth(180);
                string mapUrl = string.Format(loc.AgentIdReport.LocationMapUrl!, "600", "600");
                paragraph.AddText("\r\n");
                paragraph.AddUrlToParagraph(mapUrl, "View Full Map")
                         .SetFontSize(9F)
                         .SetFontColor(Gehtsoft.PDFFlow.Models.Shared.Color.Blue)
                         .SetUnderline();

                string imgFileName = loc.AgentIdReport!.ImageValid == true ? "yes.png" : "cancel.png";
                string matchImagePath = Path.Combine(_env.WebRootPath, "img", imgFileName);

                // 2. Create the separate text string for the match result
                string matchText = loc.AgentIdReport.ImageValid == true
                    ? $"YES\r\n({loc.AgentIdReport.Similarity}% Match)"
                    : $"NO\r\n({loc.AgentIdReport.Similarity}% No Match)";

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
            return section;
        }
    }
}