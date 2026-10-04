using Google.Cloud.Vision.V1;
using risk.control.system.Models.ViewModel;
using SkiaSharp;

namespace risk.control.system.Services.Agent
{
    public interface IGoogleMaskHelper
    {
        byte[] MaskPanTextInImage(byte[] inputImage, IReadOnlyList<TextBlock> annotations, string txt2Find);
        byte[] MaskAdharNumberTextInImage(byte[] inputImage, IReadOnlyList<TextBlock> annotations, string adharNumber);
        byte[] MaskPanTextInImage(byte[] inputImage, IReadOnlyList<EntityAnnotation> textAnnotations, string txt2Find);

        byte[] MaskPassportTextInImage(byte[] inputImage, IReadOnlyList<EntityAnnotation> textAnnotations, string passportNumber);
    }

    internal class GoogleMaskHelper : IGoogleMaskHelper
    {
        public byte[] MaskAdharNumberTextInImage(byte[] inputImage, IReadOnlyList<TextBlock> annotations, string adharNumber)
        {
            using var bitmap = SKBitmap.Decode(inputImage);
            using var canvas = new SKCanvas(bitmap);
            using var paint = new SKPaint { Color = SKColors.Black, Style = SKPaintStyle.Fill };

            // Clean input string into 4-digit tokens (e.g., ["5522", "6616", "4533"])
            var tokens = adharNumber.Split(new[] { ' ', '-' }, StringSplitOptions.RemoveEmptyEntries);

            // Set how many 4-digit blocks to mask (e.g., mask first 2 blocks -> first 8 digits)
            int blocksToMask = 2; // Set to tokens.Length if you want to mask all 12 digits

            if (tokens.Length > 0 && annotations != null && annotations.Count > 0)
            {
                // Loop through all annotations without breaking early
                for (int i = 0; i <= annotations.Count - tokens.Length; i++)
                {
                    bool matchFound = true;
                    for (int j = 0; j < tokens.Length; j++)
                    {
                        if (!annotations[i + j].Text.Equals(tokens[j], StringComparison.OrdinalIgnoreCase))
                        {
                            matchFound = false;
                            break;
                        }
                    }

                    if (matchFound)
                    {
                        // Take only the blocks you want to redact (e.g., first 2 blocks)
                        var matchedBlocks = annotations.Skip(i).Take(blocksToMask).ToList();

                        float left = matchedBlocks.Min(b => b.Left);
                        float top = matchedBlocks.Min(b => b.Top);
                        float right = matchedBlocks.Max(b => b.Right);
                        float bottom = matchedBlocks.Max(b => b.Bottom);

                        float padding = 2f;
                        var rect = new SKRect(left - padding, top - padding, right + padding, bottom + padding);

                        canvas.DrawRect(rect, paint);

                        // Advance index by token length to avoid overlapping checks on the same match
                        i += tokens.Length - 1;
                    }
                }
            }

            using var image = SKImage.FromBitmap(bitmap);
            using var data = image.Encode(SKEncodedImageFormat.Png, 100);
            return data.ToArray();
        }

        public byte[] MaskPanTextInImage(byte[] inputImage, IReadOnlyList<TextBlock> annotations, string txt2Find)
        {
            using var bitmap = SKBitmap.Decode(inputImage);
            using var canvas = new SKCanvas(bitmap);
            var paint = new SKPaint { Color = SKColors.Black, Style = SKPaintStyle.Fill };

            // The first element in Google Vision results is usually the full block of text
            var fullText = annotations.FirstOrDefault()?.Text ?? "";
            var index = fullText.IndexOf(txt2Find);

            if (index == -1) return inputImage;

            // Extract the PAN (assuming 10 chars based on your logic)
            var panNumber = fullText.Substring(index + txt2Find.Length).Trim().Split('\n')[0].Take(10);
            var panString = new string(panNumber.ToArray());

            // Find the specific block matching the PAN
            var target = annotations.FirstOrDefault(a => a.Text.Equals(panString, StringComparison.OrdinalIgnoreCase));
            if (target != null)
            {
                // Calculate the width of single character based on the 10-character box width
                float totalWidth = target.Right - target.Left;
                float charWidth = totalWidth / 10f;

                // Cover the first 6 characters (Left to Left + 6 * charWidth)
                float maskRight = target.Left + (charWidth * 6f);

                var rect = new SKRect(target.Left, target.Top, maskRight, target.Bottom);
                canvas.DrawRect(rect, paint);
            }
            using var image = SKImage.FromBitmap(bitmap);
            using var data = image.Encode(SKEncodedImageFormat.Png, 100);
            return data.ToArray();
        }
        public byte[] MaskPanTextInImage(byte[] inputImage, IReadOnlyList<EntityAnnotation> textAnnotations, string txt2Find)
        {
            using var bitmap = SKBitmap.Decode(inputImage);
            using var canvas = new SKCanvas(bitmap);
            var paint = new SKPaint
            {
                Color = SKColors.Black, // Mask color
                Style = SKPaintStyle.Fill
            };

            var allText = textAnnotations.FirstOrDefault()!.Description;

            var panTextPre = allText.IndexOf(txt2Find);

            var panNumber = allText.Substring(panTextPre + txt2Find.Length + 1, 10);

            var annotation = textAnnotations.FirstOrDefault(t => t.Description.Trim().Equals(panNumber.Trim(), StringComparison.CurrentCultureIgnoreCase));
            if (annotation is null)
            {
                return inputImage;
            }
            var allVertices = annotation.BoundingPoly.Vertices;

            var left = allVertices[0].X;

            var top = allVertices[1].Y;

            var right = allVertices[2].X;

            var bottom = allVertices[3].Y;

            var rect = new SKRect(left, top, right, bottom);

            canvas.DrawRect(rect, paint);

            using var image = SKImage.FromBitmap(bitmap);
            using var data = image.Encode(SKEncodedImageFormat.Png, 100);
            using var ms = new MemoryStream();
            data.SaveTo(ms);
            return ms.ToArray();
        }

        public byte[] MaskPassportTextInImage(byte[] inputImage, IReadOnlyList<EntityAnnotation> textAnnotations, string passportNumber)
        {
            using (var bitmap = SKBitmap.Decode(inputImage))
            using (var canvas = new SKCanvas(bitmap))
            {
                var paint = new SKPaint
                {
                    Color = SKColors.Black, // Mask color
                    Style = SKPaintStyle.Fill
                };

                var annotation = textAnnotations.FirstOrDefault(t => t.Description.Trim().Equals(passportNumber.Trim(), StringComparison.CurrentCultureIgnoreCase));

                var allVertices = annotation!.BoundingPoly.Vertices;

                var left = allVertices[0].X;

                var top = allVertices[1].Y;

                var right = allVertices[2].X;

                var bottom = allVertices[3].Y;

                var rect = new SKRect(left, top, right, bottom);

                canvas.DrawRect(rect, paint);

                using (var image = SKImage.FromBitmap(bitmap))
                using (var data = image.Encode(SKEncodedImageFormat.Png, 100))
                using (var ms = new MemoryStream())
                {
                    data.SaveTo(ms);
                    return ms.ToArray();
                }
            }
        }
    }
}