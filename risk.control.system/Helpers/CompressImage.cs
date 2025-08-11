using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Processing;

using Color = SixLabors.ImageSharp.Color;
using Font = SixLabors.Fonts.Font;
using PointF = SixLabors.ImageSharp.PointF;
using Size = SixLabors.ImageSharp.Size;

namespace risk.control.system.Helpers
{
    public static class CompressImage
    {
        public static byte[] ProcessCompress(byte[] imageByte, string onlyExtension, float cornerRadius = 10, int quality = 99, Amazon.Rekognition.Model.BoundingBox? faceBox = null)
        {
            using var stream = new MemoryStream(imageByte);
            using var image = Image.Load(stream);
            using var waterMarkedImage = image.Clone(ctx => ctx.ApplyScalingWaterMark());
            using MemoryStream streamOut = new MemoryStream();
            if (onlyExtension == ".png")
            {
                var pngEncoder = new PngEncoder
                {
                    CompressionLevel = PngCompressionLevel.BestCompression
                };
                waterMarkedImage.Save(streamOut, pngEncoder);
            }
            else if (onlyExtension == ".jpg" || onlyExtension == ".jpeg")
            {
                var jpgEncoder = new JpegEncoder
                {
                    Quality = quality, // Adjust this value for desired compression quality
                };
                waterMarkedImage.Save(streamOut, jpgEncoder);
            }
            var imageOutByte = streamOut.ToArray();
            return imageOutByte;
        }

        private static IImageProcessingContext ApplyScalingWaterMark(this IImageProcessingContext processingContext)
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