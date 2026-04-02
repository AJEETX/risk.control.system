using Google.Cloud.Vision.V1;
using risk.control.system.Models.ViewModel;
using SkiaSharp;

namespace risk.control.system.Services.Agent
{
    public interface IGoogleMaskHelper
    {
        byte[] MaskPanTextInImage(byte[] inputImage, IReadOnlyList<TextBlock> annotations, string txt2Find);
        byte[] MaskPanTextInImage(byte[] inputImage, IReadOnlyList<EntityAnnotation> textAnnotations, string txt2Find);

        byte[] MaskPassportTextInImage(byte[] inputImage, IReadOnlyList<EntityAnnotation> textAnnotations, string passportNumber);
    }

    internal class GoogleMaskHelper : IGoogleMaskHelper
    {
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
                var rect = new SKRect(target.Left, target.Top, target.Right, target.Bottom);
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

                //foreach (var vertex in annotation.BoundingPoly.Vertices)
                //{
                //    // Assuming each vertex represents a point in the bounding box
                //    // Convert vertices to SkiaSharp points
                //    var skiaPoint = new SKPoint(vertex.X, vertex.Y);

                //    // Draw a rectangle around the text to mask it
                //    canvas.DrawRect(skiaPoint.X, skiaPoint.Y, skiaPoint.X + 100, skiaPoint.Y + 50, paint);
                //}

                //foreach (var textAnnotation in textAnnotations)
                //{
                //    foreach (var vertex in textAnnotation.BoundingPoly.Vertices)
                //    {
                //        // Assuming each vertex represents a point in the bounding box
                //        // Convert vertices to SkiaSharp points
                //        var skiaPoint = new SKPoint(vertex.X, vertex.Y);

                //        // Draw a rectangle around the text to mask it
                //        canvas.DrawRect(skiaPoint.X, skiaPoint.Y, skiaPoint.X + 100, skiaPoint.Y + 50, paint);
                //    }
                //}

                // Save the modified image
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