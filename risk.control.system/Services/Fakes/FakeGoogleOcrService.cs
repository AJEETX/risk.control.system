using risk.control.system.Models.ViewModel;
using risk.control.system.Services.Tool;

namespace risk.control.system.Services.Fakes
{
    /// <summary>
    /// Canned OCR text shared by the fake Google Vision and fake AWS Textract. It satisfies every document
    /// check, so any uploaded document is VALID:
    /// Aadhaar (12-digit number, AdharCardService), Death certificate ("death" + "registration" + "date",
    /// DeathCertificateService), Income tax ("income" + "tax"/"return" + "date", IncomeTaxService);
    /// other document types only need non-empty text.
    /// </summary>
    internal static class FakeOcrText
    {
        public static readonly string[] Lines =
        {
            "E2E SAMPLE DOCUMENT",
            "AADHAAR 1234 5678 9012",
            "CERTIFICATE OF DEATH REGISTRATION",
            "INCOME TAX RETURN",
            "DATE OF REGISTRATION 01/01/2024",
            "NAME JOHN CITIZEN",
        };
    }

    /// <summary>
    /// Development/E2E stand-in for Google Cloud Vision OCR. Registered only when
    /// USE_FAKE_EXTERNAL_SERVICES=true in Development.
    /// Like Google, the first block is the full text and the following blocks are the individual words.
    /// </summary>
    internal sealed class FakeGoogleOcrService : IGoogleOcrService
    {
        public Task<IReadOnlyList<TextBlock>> DetectText(string imagePath)
        {
            var blocks = new List<TextBlock> { new(string.Join("\n", FakeOcrText.Lines), 10, 10, 610, 330) };

            var top = 10;
            foreach (var line in FakeOcrText.Lines)
            {
                var left = 10;
                foreach (var word in line.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                {
                    var width = word.Length * 14;
                    blocks.Add(new TextBlock(word, left, top, left + width, top + 30));
                    left += width + 10;
                }

                top += 80;
            }

            return Task.FromResult<IReadOnlyList<TextBlock>>(blocks);
        }

        public Task<string> DetectTextAsync(string imagePath) => Task.FromResult(string.Join("\n", FakeOcrText.Lines));
    }
}
