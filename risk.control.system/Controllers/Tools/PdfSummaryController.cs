using System.Text;

using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

using risk.control.system.Models;
using risk.control.system.Models.ViewModel;
using risk.control.system.Services;

using static risk.control.system.AppConstant.Applicationsettings;

namespace risk.control.system.Controllers.Tools;

[Authorize(Roles = GUEST.DISPLAY_NAME)]
public class PdfSummaryController : Controller
{
    private readonly ITextAnalyticsService textAnalyticsService;
    private readonly UserManager<ApplicationUser> _userManager; // Add UserManager

    public PdfSummaryController(
        ITextAnalyticsService textAnalyticsService,
        UserManager<ApplicationUser> userManager) // Inject UserManager
    {
        this.textAnalyticsService = textAnalyticsService;
        this._userManager = userManager;
    }

    public async Task<IActionResult> Index()
    {
        var user = await _userManager.GetUserAsync(User);

        var model = new PdfSummaryViewModel
        {
            RemainingTries = 5 - (user?.PdfCount ?? 0)
        };

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Summarize(IFormFile pdfFile)
    {
        if (pdfFile == null || pdfFile.Length == 0)
        {
            return BadRequest(new { errorMessage = "Please upload a valid PDF file." });
        }

        // 1. Get current user and verify limit
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();

        if (user.PdfCount >= 5)
        {
            return StatusCode(403, new { errorMessage = "PDF Summary limit reached (5/5). Try again tomorrow." });
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

            // 2. Run the AI Service
            var summary = await textAnalyticsService.AbstractiveSummarizeAsync(content);

            // 3. Increment the count in the database
            user.PdfCount++;
            await _userManager.UpdateAsync(user);

            // 4. Return summary and current remaining count
            return Ok(new
            {
                summary = summary,
                remaining = 5 - user.PdfCount
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { errorMessage = "Internal server error: " + ex.Message });
        }
    }
}