//using SkiaSharp;

//namespace risk.control.system.Helpers
//{
//    public static class ImageCompression
//    {
//        private static int Width = 800;
//        private static int Height = 600;

//        public static byte[] ConverterSkia(byte[] imageBytes, int maxquality = 100)
//        {
//            if (imageBytes.Length > 1 * 1024)
//            {
//                var stream = new MemoryStream(imageBytes);
//                using var skData = SKData.Create(stream);
//                using var codec = SKCodec.Create(skData);

//                var supportedScale = codec.GetScaledDimensions((float)Width / codec.Info.Width);

//                var nearest = new SKImageInfo(supportedScale.Width, supportedScale.Height);
//                using var destinationImage = SKBitmap.Decode(codec, nearest);
//                using var resizedImage = destinationImage.Resize(new SKImageInfo(Width, Height), SKFilterQuality.High);

//                var format = SKEncodedImageFormat.Jpeg;
//                using var outputImage = SKImage.FromBitmap(resizedImage);
//                using var data = outputImage.Encode(format, maxquality);

//                using var memoryStream = new MemoryStream();
//                data.SaveTo(memoryStream);
//                return memoryStream.ToArray();

//                //using var outputStream = GetOutputStream("skiasharp");
//                //data.SaveTo(outputStream);

//                //using var ms = new MemoryStream();
//                //outputStream.CopyTo(ms);
//                //var bytes = ms.ToArray();
//                //outputStream.Close();
//                //stream.Close();
//                //return bytes;
//            }
//            return imageBytes;
//        }

//        public static byte[] ConverterSkiaResize(byte[] imageBytes, int maxquality = 100)
//        {
//            var resizeFactor = 0.5f;
//            var bitmap = SKBitmap.Decode(imageBytes);
//            var toBitmap = new SKBitmap((int)Math.Round(bitmap.Width * resizeFactor), (int)Math.Round(bitmap.Height * resizeFactor), bitmap.ColorType, bitmap.AlphaType);

//            var canvas = new SKCanvas(toBitmap);
//            // Draw a bitmap rescaled
//            canvas.SetMatrix(SKMatrix.MakeScale(resizeFactor, resizeFactor));
//            canvas.DrawBitmap(bitmap, 0, 0);
//            canvas.ResetMatrix();

//            var font = SKTypeface.FromFamilyName("Arial");
//            var brush = new SKPaint
//            {
//                Typeface = font,
//                TextSize = 45.0f,
//                IsAntialias = true,
//                Color = new SKColor(255, 255, 255, 255)
//            };
//            canvas.DrawText("iCheckified!", bitmap.Width * resizeFactor / 3.0f, bitmap.Height * resizeFactor / 1.05f, brush);

//            canvas.Flush();

//            var image = SKImage.FromBitmap(toBitmap);
//            var data = image.Encode(SKEncodedImageFormat.Jpeg, maxquality);

//            using var memoryStream = new MemoryStream();
//            data.SaveTo(memoryStream);

//            using (var stream = new FileStream("output.jpg", FileMode.Create, FileAccess.Write))
//                data.SaveTo(stream);

//            data.Dispose();
//            image.Dispose();
//            canvas.Dispose();
//            brush.Dispose();
//            font.Dispose();
//            toBitmap.Dispose();
//            bitmap.Dispose();
//            return memoryStream.ToArray();
//        }
//    }
//}