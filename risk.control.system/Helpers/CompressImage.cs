using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using Size = SixLabors.ImageSharp.Size;
using Color = SixLabors.ImageSharp.Color;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.Fonts;
using Font = SixLabors.Fonts.Font;
using PointF = SixLabors.ImageSharp.PointF;
using Brushes = SixLabors.ImageSharp.Drawing.Processing.Brushes;
using Pens = SixLabors.ImageSharp.Drawing.Processing.Pens;

namespace risk.control.system.Helpers
{
    public static class CompressImage
    {
        //public static byte[] CompressimageWindowsStream(Stream srcImgStream)
        //{
        //    try
        //    {
        //        // Convert stream to image
        //        //using var image = System.Drawing.Image.FromStream(srcImgStream);

        //        float maxHeight = 900.0f;
        //        float maxWidth = 900.0f;
        //        int newWidth;
        //        int newHeight;

        //        var originalBMP = new Bitmap(srcImgStream);
        //        int originalWidth = originalBMP.Width;
        //        int originalHeight = originalBMP.Height;

        //        if (originalWidth > maxWidth || originalHeight > maxHeight)
        //        {
        //            // To preserve the aspect ratio
        //            float ratioX = (float)maxWidth / (float)originalWidth;
        //            float ratioY = (float)maxHeight / (float)originalHeight;
        //            float ratio = Math.Min(ratioX, ratioY);
        //            newWidth = (int)(originalWidth * ratio);
        //            newHeight = (int)(originalHeight * ratio);
        //        }
        //        else
        //        {
        //            newWidth = (int)originalWidth;
        //            newHeight = (int)originalHeight;
        //        }

        //        var bitmap = new Bitmap(originalBMP, newWidth, newHeight);
        //        var imgGraph = Graphics.FromImage(bitmap);

        //        imgGraph.SmoothingMode = SmoothingMode.Default;
        //        imgGraph.InterpolationMode = InterpolationMode.HighQualityBicubic;
        //        imgGraph.DrawImage(originalBMP, 0, 0, newWidth, newHeight);
        //        using var ms = new MemoryStream();
        //        bitmap.Save(ms, ImageFormat.Png);
        //        bitmap.Dispose();
        //        imgGraph.Dispose();
        //        originalBMP.Dispose();
        //        return ms.ToArray();
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine(ex.Message);
        //        return null;
        //    }
        //}

        //public static void CompressimageWindows(Stream srcImgStream, string targetPath)
        //{
        //    try
        //    {
        //        // Convert stream to image
        //        using var image = System.Drawing.Image.FromStream(srcImgStream);

        //        float maxHeight = 900.0f;
        //        float maxWidth = 900.0f;
        //        int newWidth;
        //        int newHeight;

        //        var originalBMP = new Bitmap(srcImgStream);
        //        int originalWidth = originalBMP.Width;
        //        int originalHeight = originalBMP.Height;

        //        if (originalWidth > maxWidth || originalHeight > maxHeight)
        //        {
        //            // To preserve the aspect ratio
        //            float ratioX = (float)maxWidth / (float)originalWidth;
        //            float ratioY = (float)maxHeight / (float)originalHeight;
        //            float ratio = Math.Min(ratioX, ratioY);
        //            newWidth = (int)(originalWidth * ratio);
        //            newHeight = (int)(originalHeight * ratio);
        //        }
        //        else
        //        {
        //            newWidth = (int)originalWidth;
        //            newHeight = (int)originalHeight;
        //        }

        //        var bitmap = new Bitmap(originalBMP, newWidth, newHeight);
        //        var imgGraph = Graphics.FromImage(bitmap);

        //        imgGraph.SmoothingMode = SmoothingMode.Default;
        //        imgGraph.InterpolationMode = InterpolationMode.HighQualityBicubic;
        //        imgGraph.DrawImage(originalBMP, 0, 0, newWidth, newHeight);

        //        var extension = System.IO.Path.GetExtension(targetPath).ToLower();
        //        // for file extension having png and gif
        //        if (extension == ".png" || extension == ".gif")
        //        {
        //            // Save image to targetPath
        //            bitmap.Save(targetPath, image.RawFormat);
        //        }

        //        // for file extension having .jpg or .jpeg
        //        else if (extension == ".jpg" || extension == ".jpeg")
        //        {
        //            ImageCodecInfo jpgEncoder = GetEncoder(ImageFormat.Jpeg);
        //            Encoder myEncoder = Encoder.Quality;
        //            var encoderParameters = new EncoderParameters(1);
        //            var parameter = new EncoderParameter(myEncoder, 50L);
        //            encoderParameters.Param[0] = parameter;

        //            // Save image to targetPath
        //            bitmap.Save(targetPath, jpgEncoder, encoderParameters);
        //        }
        //        bitmap.Dispose();
        //        imgGraph.Dispose();
        //        originalBMP.Dispose();
        //    }
        //    catch (Exception ex)
        //    {
        //        throw ex;
        //    }
        //}

        //private static ImageCodecInfo GetEncoder(ImageFormat format)
        //{
        //    ImageCodecInfo[] codecs = ImageCodecInfo.GetImageDecoders();
        //    foreach (ImageCodecInfo codec in codecs)
        //    {
        //        if (codec.FormatID == format.Guid)
        //        {
        //            return codec;
        //        }
        //    }
        //    return null;
        //}

        //public static byte[] Compress(byte[] imageByte, int quality = 99)
        //{
        //    using var stream = new MemoryStream(imageByte);
        //    using var image = SixLabors.ImageSharp.Image.Load(stream);

        //    var encoder = new JpegEncoder
        //    {
        //        Quality = quality, // Adjust this value for desired compression quality
        //    };
        //    using MemoryStream streamOut = new MemoryStream();

        //    image.Save(streamOut, encoder);

        //    var imageOutByte = streamOut.ToArray();
        //    return imageOutByte;
        //}

        //public static byte[] ProcessCompressBlur(byte[] imageByte, float cornerRadius = 10, int quality = 99)
        //{
        //    using var stream = new MemoryStream(imageByte);
        //    using var image = SixLabors.ImageSharp.Image.Load(stream);
        //    float maxHeight = 1800.0f;
        //    float maxWidth = 1800.0f;
        //    float newWidth;
        //    float newHeight;

        //    if (image.Width > maxWidth || image.Height > maxHeight)
        //    {
        //        // To preserve the aspect ratio
        //        float ratioX = (float)maxWidth / (float)image.Width;
        //        float ratioY = (float)maxHeight / (float)image.Height;
        //        float ratio = Math.Min(ratioX, ratioY);
        //        newWidth = (image.Width * ratio);
        //        newHeight = (image.Height * ratio);
        //    }
        //    else
        //    {
        //        newWidth = (int)image.Width;
        //        newHeight = (int)image.Height;
        //    }

        //    image.Mutate(x => x.Resize(image.Width / 2, image.Height / 2, KnownResamplers.Triangle));
        //    var encoder = new JpegEncoder
        //    {
        //        Quality = quality, // Adjust this value for desired compression quality
        //    };

        //    //using var roundImage = image.Clone(x => x.ConvertToAvatar(new Size(image.Width, image.Height), cornerRadius));

        //    Font font = SixLabors.Fonts.SystemFonts.CreateFont("Arial", 1, SixLabors.Fonts.FontStyle.Bold);

        //    // The options are optional
        //    RichTextOptions options = new(font)
        //    {
        //        Origin = new PointF(500, 100), // Set the rendering origin.
        //        TabWidth = 1, // A tab renders as 8 spaces wide
        //        WrappingLength = 0, // Greater than zero so we will word wrap at 100 pixels wide
        //        HorizontalAlignment = HorizontalAlignment.Left // Right align
        //    };

        //    using var waterMarkedImage = image.Clone(ctx => ctx.ApplyScalingWaterMarkSimple(font, "XXXXXXXXXX", Color.Black, 5));

        //    using MemoryStream streamOut = new MemoryStream();

        //    waterMarkedImage.Save(streamOut, encoder);
        //    var imageOutByte = streamOut.ToArray();
        //    return imageOutByte;
        //}
        public static byte[] ProcessCompress(byte[] imageByte, float cornerRadius = 10, int quality = 99)
        {
            using var stream = new MemoryStream(imageByte);
            using var image = SixLabors.ImageSharp.Image.Load(stream);
            float maxHeight = 1800.0f;
            float maxWidth = 1800.0f;
            float newWidth;
            float newHeight;

            if (image.Width > maxWidth || image.Height > maxHeight)
            {
                // To preserve the aspect ratio
                float ratioX = (float)maxWidth / (float)image.Width;
                float ratioY = (float)maxHeight / (float)image.Height;
                float ratio = Math.Min(ratioX, ratioY);
                newWidth = (image.Width * ratio);
                newHeight = (image.Height * ratio);
            }
            else
            {
                newWidth = (int)image.Width;
                newHeight = (int)image.Height;
            }

            image.Mutate(x => x.Resize(image.Width / 2, image.Height / 2, KnownResamplers.Triangle));
            var encoder = new JpegEncoder
            {
                Quality = quality, // Adjust this value for desired compression quality
            };
            using MemoryStream streamOut = new MemoryStream();

            using var roundImage = image.Clone(x => x.ConvertToAvatar(new Size(image.Width, image.Height), cornerRadius));

            Font font = SixLabors.Fonts.SystemFonts.CreateFont("Arial", 2, SixLabors.Fonts.FontStyle.Italic);

            // The options are optional
            RichTextOptions options = new(font)
            {
                Origin = new PointF(100, 100), // Set the rendering origin.
                TabWidth = 8, // A tab renders as 8 spaces wide
                WrappingLength = 0, // Greater than zero so we will word wrap at 100 pixels wide
                HorizontalAlignment = HorizontalAlignment.Right // Right align
            };

            PatternBrush brush = Brushes.Horizontal(Color.Red, Color.Blue);
            PatternPen pen = Pens.DashDot(Color.Green, 5);

            //roundImage.Mutate(x => x.DrawText(options, "scanned and processed", brush, pen));

            using var waterMarkedImage = roundImage.Clone(ctx => ctx.ApplyScalingWaterMark(font, "...iCheckified...", Color.Silver, 30, false));

            waterMarkedImage.Save(streamOut, encoder);
            var imageOutByte = streamOut.ToArray();
            return imageOutByte;
        }

        // Implements a full image mutating pipeline operating on IImageProcessingContext
        private static IImageProcessingContext ConvertToAvatar(this IImageProcessingContext context, Size size, float cornerRadius)
        {
            return context.Resize(new ResizeOptions
            {
                Size = size,
                Mode = ResizeMode.Crop
            }).ApplyRoundedCorners(cornerRadius);
        }

        // This method can be seen as an inline implementation of an `IImageProcessor`:
        // (The combination of `IImageOperations.Apply()` + this could be replaced with an `IImageProcessor`)
        private static IImageProcessingContext ApplyRoundedCorners(this IImageProcessingContext context, float cornerRadius)
        {
            Size size = context.GetCurrentSize();
            IPathCollection corners = BuildCorners(size.Width, size.Height, cornerRadius);

            context.SetGraphicsOptions(new GraphicsOptions()
            {
                Antialias = true,

                // Enforces that any part of this shape that has color is punched out of the background
                AlphaCompositionMode = PixelAlphaCompositionMode.DestOut
            });

            // Mutating in here as we already have a cloned original
            // use any color (not Transparent), so the corners will be clipped
            foreach (IPath path in corners)
            {
                context = context.Fill(Color.Red, path);
            }

            return context;
        }

        private static IPathCollection BuildCorners(int imageWidth, int imageHeight, float cornerRadius)
        {
            // First create a square
            var rect = new RectangularPolygon(-0.5f, -0.5f, cornerRadius, cornerRadius);

            // Then cut out of the square a circle so we are left with a corner
            IPath cornerTopLeft = rect.Clip(new EllipsePolygon(cornerRadius - 0.5f, cornerRadius - 0.5f, cornerRadius));

            // Corner is now a corner shape positions top left
            // let's make 3 more positioned correctly, we can do that by translating the original around the center of the image.

            float rightPos = imageWidth - cornerTopLeft.Bounds.Width + 1;
            float bottomPos = imageHeight - cornerTopLeft.Bounds.Height + 1;

            // Move it across the width of the image - the width of the shape
            IPath cornerTopRight = cornerTopLeft.RotateDegree(90).Translate(rightPos, 0);
            IPath cornerBottomLeft = cornerTopLeft.RotateDegree(-90).Translate(0, bottomPos);
            IPath cornerBottomRight = cornerTopLeft.RotateDegree(180).Translate(rightPos, bottomPos);

            return new PathCollection(cornerTopLeft, cornerBottomLeft, cornerTopRight, cornerBottomRight);
        }

        private static IImageProcessingContext ApplyScalingWaterMark(this IImageProcessingContext processingContext, Font font, string text, Color color, float padding, bool wordwrap)
        {
            if (wordwrap)
            {
                return processingContext.ApplyScalingWaterMarkWordWrap(font, text, color, padding);
            }
            else
            {
                return processingContext.ApplyScalingWaterMarkSimple(font, text, color, padding);
            }
        }

        private static IImageProcessingContext ApplyScalingWaterMarkSimple(this IImageProcessingContext processingContext,
            Font font,
            string text,
            Color color,
            float padding)
        {
            Size imgSize = processingContext.GetCurrentSize();

            float targetWidth = imgSize.Width - (padding * 2);
            float targetHeight = imgSize.Height - (padding * 2);

            // Measure the text size
            FontRectangle size = TextMeasurer.MeasureSize(text, new TextOptions(font));

            // Find out how much we need to scale the text to fill the space (up or down)
            float scalingFactor = Math.Min(targetWidth / size.Width, targetHeight / size.Height);

            // Create a new font
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

        private static IImageProcessingContext ApplyScalingWaterMarkWordWrap(this IImageProcessingContext processingContext,
            Font font,
            string text,
            Color color,
            float padding)
        {
            Size imgSize = processingContext.GetCurrentSize();
            float targetWidth = imgSize.Width - (padding * 2);
            float targetHeight = imgSize.Height - (padding * 2);

            float targetMinHeight = imgSize.Height - (padding * 3); // Must be with in a margin width of the target height

            // Now we are working in 2 dimensions at once and can't just scale because it will cause the text to
            // reflow we need to just try multiple times
            var scaledFont = font;
            FontRectangle s = new FontRectangle(0, 0, float.MaxValue, float.MaxValue);

            float scaleFactor = (scaledFont.Size / 2); // Every time we change direction we half this size
            int trapCount = (int)scaledFont.Size * 2;
            if (trapCount < 10)
            {
                trapCount = 10;
            }

            bool isTooSmall = false;

            while ((s.Height > targetHeight || s.Height < targetMinHeight) && trapCount > 0)
            {
                if (s.Height > targetHeight)
                {
                    if (isTooSmall)
                    {
                        scaleFactor /= 2;
                    }

                    scaledFont = new Font(scaledFont, scaledFont.Size - scaleFactor);
                    isTooSmall = false;
                }

                if (s.Height < targetMinHeight)
                {
                    if (!isTooSmall)
                    {
                        scaleFactor /= 2;
                    }
                    scaledFont = new Font(scaledFont, scaledFont.Size + scaleFactor);
                    isTooSmall = true;
                }
                trapCount--;

                s = TextMeasurer.MeasureSize(text, new TextOptions(scaledFont)
                {
                    WrappingLength = targetWidth
                });
            }

            var center = new PointF(padding, imgSize.Height / 2);
            var textOptions = new RichTextOptions(scaledFont)
            {
                Origin = center,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Center,
                WrappingLength = targetWidth
            };
            return processingContext.DrawText(textOptions, text, color);
        }
    }
}