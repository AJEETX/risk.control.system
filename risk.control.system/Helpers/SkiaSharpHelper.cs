using Amazon.Textract.Model;

using SkiaSharp;

namespace risk.control.system.Helpers
{
    public class SkiaSharpHelper
    {
        private static string txt2Find = "Permanent Account Number";

        public static byte[] MaskTextInImage(byte[] inputImage, List<Block> textBlocks)
        {
            var bitmap = SKBitmap.Decode(inputImage);

            using (var canvas = new SKCanvas(bitmap))
            {
                var paint = new SKPaint
                {
                    Color = SKColors.Black, // Mask color
                    Style = SKPaintStyle.Fill
                };

                var hasPanLabel = textBlocks[9].Text == txt2Find;
                if(hasPanLabel)
                {
                    var block = textBlocks[10];
                    var boundingBox = block.Geometry.BoundingBox;
                    var rect = new SKRect(
                        boundingBox.Left * bitmap.Width,
                        boundingBox.Top * bitmap.Height,
                        (boundingBox.Left + boundingBox.Width) * bitmap.Width,
                        (boundingBox.Top + boundingBox.Height) * bitmap.Height);

                    canvas.DrawRect(rect, paint);
                }

                //foreach (var block in textBlocks)
                //{
                //    if (block.BlockType == "LINE" && block.Geometry != null)
                //    {
                //        var boundingBox = block.Geometry.BoundingBox;
                //        var rect = new SKRect(
                //            boundingBox.Left * bitmap.Width,
                //            boundingBox.Top * bitmap.Height,
                //            (boundingBox.Left + boundingBox.Width) * bitmap.Width,
                //            (boundingBox.Top + boundingBox.Height) * bitmap.Height);

                //        canvas.DrawRect(rect, paint);
                //    }
                //}
            }

            using (var image = SKImage.FromBitmap(bitmap))
            using (var data = image.Encode(SKEncodedImageFormat.Png, 100))
            using (var ms = new MemoryStream())
            {
                data.SaveTo(ms);
                return ms.ToArray();
            }
        }
        public static byte[] GetMaskedImage(byte[] image)
        {
            var image2Process = SKBitmap.Decode(image);

            var mask = CreateTextMask(300, 30, "XXXXXXXXX", SKTypeface.Default, 10);

            var maskedImage = ApplyMask(image2Process, mask);

            var maskedbyteImage = SaveImage(maskedImage);
            
            return maskedbyteImage;

        }

        private static SKBitmap CreateTextMask(int width, int height, string text, SKTypeface typeface, float textSize)
        {
            SKBitmap mask = new SKBitmap(width, height);
            using (var canvas = new SKCanvas(mask))
            {
                canvas.Clear(SKColors.Transparent);

                using (var paint = new SKPaint())
                {
                    paint.Typeface = typeface;
                    paint.TextSize = textSize;
                    paint.Color = SKColors.White; // Color doesn't matter, just the alpha
                    paint.IsAntialias = true;

                    // Calculate text bounds
                    SKRect textBounds = new SKRect();
                    paint.MeasureText(text, ref textBounds);

                    // Draw the text centered
                    float xText = (width - textBounds.Width) / 2 - textBounds.Left;
                    float yText = (height - textBounds.Height) / 2 - textBounds.Top;

                    canvas.DrawText(text, xText, yText, paint);
                }
            }

            return mask;
        }
        private static SKBitmap ApplyMask(SKBitmap image, SKBitmap mask)
        {
            SKBitmap result = new SKBitmap(image.Width, image.Height);
            using (var canvas = new SKCanvas(result))
            {
                canvas.Clear(SKColors.Black);

                using (var paint = new SKPaint())
                {
                    paint.BlendMode = SKBlendMode.SrcIn; // Only keep the intersection of source (mask) and destination (image)
                    paint.Color = SKColors.Black;
                    paint.FilterQuality = SKFilterQuality.High;
                    // Draw the image
                    canvas.DrawBitmap(image, 0, 0);

                    // Draw the mask
                    canvas.DrawBitmap(mask, 30, 420, paint);
                }
            }

            return result;
        }
        private static byte[] SaveImage(SKBitmap bitmap)
        {
            using (var image = SKImage.FromBitmap(bitmap))
            using (var data = image.Encode(SKEncodedImageFormat.Png, 100))
            using (var ms = new MemoryStream())
            {
                data.SaveTo(ms);
                return ms.ToArray();
            }



            //using (var stream = File.OpenWrite(filePath))
            //{
            //    data.SaveTo(stream);
            //    using var ms = new MemoryStream();
            //    stream.CopyTo(ms);
            //    return ms.ToArray();
            //}
        }
    }
}
