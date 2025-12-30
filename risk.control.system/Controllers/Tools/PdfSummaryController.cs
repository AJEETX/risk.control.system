using System.Text;

using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;

using Microsoft.AspNetCore.Mvc;

using risk.control.system.Services;

namespace risk.control.system.Controllers.Tools;

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
            return BadRequest("Please upload a PDF.");
        }
        try
        {
            string content;
            // Check if the file is a PDF
            if (pdfFile.ContentType == "application/pdf")
            {
                // Extract text from the PDF
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
            else
            {
                // For non-PDF files, read as plain text
                using var streamReader = new StreamReader(pdfFile.OpenReadStream());
                content = await streamReader.ReadToEndAsync();
            }

            if (string.IsNullOrWhiteSpace(content))
            {
                return BadRequest(new { message = "The file content is empty." });
            }

            // Summarize the document
            var summary = await textAnalyticsService.AbstractiveSummarizeAsync(content);
            ViewBag.Summary = summary;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
            throw;
        }

        return View("Index");
    }
}
