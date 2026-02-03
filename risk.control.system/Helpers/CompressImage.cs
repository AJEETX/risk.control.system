using SixLabors.Fonts;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Processing;

using Color = SixLabors.ImageSharp.Color;
using Font = SixLabors.Fonts.Font;
using PointF = SixLabors.ImageSharp.PointF;
using Size = SixLabors.ImageSharp.Size;

namespace risk.control.system.Helpers
{
    public static class CompressImageExtension
    {
        public static IImageProcessingContext ApplyScalingWaterMark(this IImageProcessingContext processingContext)
        {
            string text = "...iCheckified...";
            var padding = 30;
            Color color = Color.Silver;
            Font font = SystemFonts.CreateFont("Arial", 2, FontStyle.Italic);
            Size imgSize = processingContext.GetCurrentSize();
            float targetWidth = imgSize.Width - (padding * 2);
            float targetHeight = imgSize.Height - (padding * 2);

            FontRectangle size = TextMeasurer.MeasureSize(text, new TextOptions(font));

            float scalingFactor = Math.Min(targetWidth / size.Width, targetHeight / size.Height);
            Font scaledFont = new Font(font, scalingFactor * font.Size);
            var center = new PointF(imgSize.Width / 2, imgSize.Height / 1.1F);
            var textOptions = new RichTextOptions(scaledFont)
            {
                Origin = center,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            return processingContext.DrawText(textOptions, text, color);
        }
    }
}