using Gehtsoft.PDFFlow.Builder;
using Gehtsoft.PDFFlow.Models.Enumerations;
using Gehtsoft.PDFFlow.Utils;
using risk.control.system.Models;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;

namespace risk.control.system.Services
{
    public interface IPdfGenerateQuestionLocationService
    {
        SectionBuilder Build(SectionBuilder section, LocationReport loc);
    }
    public class PdfGenerateQuestionLocationService : IPdfGenerateQuestionLocationService
    {
        internal static readonly FontBuilder FNT9 = Fonts.Helvetica(9f);
        internal static readonly FontBuilder FNT10 = Fonts.Helvetica(10f);
        internal static readonly FontBuilder FNT12 = Fonts.Helvetica(12f);
        internal static readonly FontBuilder FNT12B = Fonts.Helvetica(12f).SetBold(true);
        internal static readonly FontBuilder FNT20 = Fonts.Helvetica(20f);
        internal static readonly FontBuilder FNT19B = Fonts.Helvetica(19f).SetBold();

        internal static readonly FontBuilder FNT8 = Fonts.Helvetica(8f);

        internal static readonly FontBuilder FNT8_G =
            Fonts.Helvetica(8f).SetColor(Gehtsoft.PDFFlow.Models.Shared.Color.Gray);

        internal static readonly FontBuilder FNT9B =
            Fonts.Helvetica(9f).SetBold();

        internal static readonly FontBuilder FNT11B =
            Fonts.Helvetica(11f).SetBold();

        internal static readonly FontBuilder FNT15 = Fonts.Helvetica(15f);
        internal static readonly FontBuilder FNT16 = Fonts.Helvetica(16f);

        internal static readonly FontBuilder FNT16_R =
            Fonts.Helvetica(16f).SetColor(Gehtsoft.PDFFlow.Models.Shared.Color.Red);
        internal static readonly FontBuilder FNT16_G =
            Fonts.Helvetica(16f).SetColor(Gehtsoft.PDFFlow.Models.Shared.Color.Green);
        internal static readonly FontBuilder FNT17 = Fonts.Helvetica(17f);
        internal static readonly FontBuilder FNT18 = Fonts.Helvetica(18f);
        public SectionBuilder Build(SectionBuilder section, LocationReport loc)
        {
            // =================== QUESTIONS ====================
            if (loc.Questions?.Any() == true)
            {
                section.AddParagraph()
                        .SetLineSpacing(1)
                        .AddText($"Questions & Answers  : {loc.Updated.GetValueOrDefault().ToString("dd-MMM-yyyy")}")
                        .SetFontSize(14)
                        .SetBold()
                        .SetUnderline();

                // Create a simple table without any styling to check the issue
                var tableBuilder = section.AddTable()
                                          .SetBorder(Stroke.Solid);
                tableBuilder
                    .AddColumnPercentToTable("Question", 50)
                    .AddColumnPercentToTable("Answer", 50);


                foreach (var question in loc.Questions)
                {
                    var rowBuilder = tableBuilder.AddRow();
                    rowBuilder.AddCell().AddParagraph().AddText(question.QuestionText);
                    rowBuilder.AddCell().AddParagraph().AddText(question.AnswerText);
                }
            }
            return section;
        }


        public static byte[] ConvertToPng(byte[] imageBytes)
        {
            using var inputStream = new MemoryStream(imageBytes);
            using var image = Image.Load(inputStream); // Auto-detects format
            using var outputStream = new MemoryStream();
            image.Save(outputStream, new PngEncoder()); // Encode as PNG
            return outputStream.ToArray();
        }
    }
}
