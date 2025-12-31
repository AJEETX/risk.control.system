using System.Text;

using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using risk.control.system.Services;

using static risk.control.system.AppConstant.Applicationsettings;

namespace risk.control.system.Controllers.Tools;

[Authorize(Roles = GUEST.DISPLAY_NAME)]
public class PdfSummaryController : Controller
{
    private readonly ITextAnalyticsService textAnalyticsService;

    public PdfSummaryController(ITextAnalyticsService textAnalyticsService)
    {
        this.textAnalyticsService = textAnalyticsService;
    }
    public IActionResult Index() => View();

    [HttpPost]
    public async Task<IActionResult> Summarize(IFormFile pdfFile)
    {
        if (pdfFile == null || pdfFile.Length == 0)
        {
            return BadRequest(new { errorMessage = "Please upload a valid PDF file." });
        }

        try
        {
            string content = "";
            if (pdfFile.ContentType == "application/pdf")
            {
                using var memoryStream = new MemoryStream();
                await pdfFile.CopyToAsync(memoryStream);
                memoryStream.Position = 0;

                using var pdfReader = new PdfReader(memoryStream);
                using var pdfDocument = new PdfDocument(pdfReader);
                var textBuilder = new StringBuilder();

                for (int page = 1; page <= pdfDocument.GetNumberOfPages(); page++)
                {
                    textBuilder.Append(PdfTextExtractor.GetTextFromPage(pdfDocument.GetPage(page)));
                }
                content = textBuilder.ToString();
            }

            if (string.IsNullOrWhiteSpace(content))
            {
                return BadRequest(new { errorMessage = "The file content is empty." });
            }

            var summary = await textAnalyticsService.AbstractiveSummarizeAsync(content);

            // Return JSON for AJAX
            return Ok(new { summary = summary });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { errorMessage = "Internal server error: " + ex.Message });
        }
    }
}
