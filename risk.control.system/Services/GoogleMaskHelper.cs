using Google.Cloud.Vision.V1;
using SkiaSharp;
using SkiaSharp.Views.Desktop;

using System.Collections.Generic;
using System.IO;

namespace risk.control.system.Services
{
    public interface IGoogleMaskHelper
    {
        byte[] MaskPanTextInImage(byte[] inputImage, IReadOnlyList<EntityAnnotation> textAnnotations, string txt2Find);
        byte[] MaskPassportTextInImage(byte[] inputImage, IReadOnlyList<EntityAnnotation> textAnnotations, string passportNumber);
    }
    public class GoogleMaskHelper : IGoogleMaskHelper
    {
        public byte[] MaskPanTextInImage(byte[] inputImage, IReadOnlyList<EntityAnnotation> textAnnotations, string txt2Find)
        {
            using (var bitmap = SKBitmap.Decode(inputImage))
            using (var canvas = new SKCanvas(bitmap))
            {
                var paint = new SKPaint
                {
                    Color = SKColors.Black, // Mask color
                    Style = SKPaintStyle.Fill
                };


                var allText = textAnnotations.FirstOrDefault().Description;

                var panTextPre = allText.IndexOf(txt2Find);

                var panNumber = allText.Substring(panTextPre + txt2Find.Length + 1, 10);

                var annotation = textAnnotations.FirstOrDefault(t => t.Description.Trim().ToUpperInvariant() == panNumber.Trim().ToUpperInvariant());
                if(annotation is null)
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


                var annotation = textAnnotations.FirstOrDefault(t => t.Description.Trim().ToUpperInvariant() == passportNumber.Trim().ToUpperInvariant());

                var allVertices = annotation.BoundingPoly.Vertices;

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
