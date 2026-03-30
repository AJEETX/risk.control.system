using System.Text;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using risk.control.system.AppConstant;
using risk.control.system.Models;
using risk.control.system.Models.ViewModel;
using risk.control.system.Services.Tool;

namespace risk.control.system.Controllers.Tools;

[Authorize(Roles = GUEST.DISPLAY_NAME)]
public class PdfSummaryController : Controller
{
    private readonly ILogger<PdfSummaryController> logger;
    private readonly ITextAnalyticsService textAnalyticsService;
    private readonly UserManager<ApplicationUser> _userManager; // Add UserManager

    public PdfSummaryController(
        ILogger<PdfSummaryController> logger,
        ITextAnalyticsService textAnalyticsService,
        UserManager<ApplicationUser> userManager) // Inject UserManager
    {
        this.logger = logger;
        this.textAnalyticsService = textAnalyticsService;
        this._userManager = userManager;
    }

    public async Task<IActionResult> Index()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return Unauthorized("Unauthorized");
        }
        var model = new PdfSummaryViewModel
        {
            RemainingTries = 5 - (user?.PdfCount ?? 0)
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Summarize(IFormFile pdfFile)
    {
        if (pdfFile == null || pdfFile.Length == 0) return BadRequest("Please upload a PDF file.");
        if (pdfFile!.Length > 10 * 1024 * 1024) return BadRequest("File too large.");
        var extension = Path.GetExtension(pdfFile.FileName).ToLowerInvariant();
        if (extension != ".pdf") return BadRequest("Only PDF files allowed.");
        if (pdfFile.ContentType != "application/pdf") return BadRequest("Invalid file type.");
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Unauthorized("Unauthorized");
        if (user.PdfCount >= 5)
        {
            return StatusCode(403, new { errorMessage = "PDF Summary limit reached (5/5)." });
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
            user.PdfCount++;
            await _userManager.UpdateAsync(user);
            return Ok(new { summary = summary, remaining = 5 - user.PdfCount });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing PDF summary. {UserId}", _userManager.GetUserId(User) ?? "Anonymous");
            return Ok(new { summary = "Error occurred", remaining = 5 - user.PdfCount });
        }
    }
}