//using ImageMagick;

using SkiaSharp;

namespace risk.control.system.Helpers
{
    public static class ImageCompression
    {
        private static int Width = 800;
        private static int Height = 600;

        //public static byte[] Converter(byte[] imageBytes, int maxquality = 100)
        //{
        //    try
        //    {
        //        if (imageBytes.Length > 500 * 1024)
        //        {
        //            byte[] optimizedImageBytes = OptimizeImage(imageBytes, maxquality * 1024);
        //            return optimizedImageBytes;
        //        }
        //        return imageBytes;
        //    }
        //    catch (Exception e)
        //    {
        //        throw new Exception("Erro durante a compressão da imagem: " + e.Message);
        //    }
        //}

        //private static byte[] OptimizeImage(byte[] imageBytes, long maxSizeBytes)
        //{
        //    try
        //    {
        //        using var imageStream = new MemoryStream(imageBytes);
        //        using MagickImage image = new MagickImage(imageStream);
        //        image.Resize(new MagickGeometry(800, 600));

        //        int desiredQuality = 85;

        //        while (true)
        //        {
        //            byte[] tempBytes = image.ToByteArray(MagickFormat.Jpg);

        //            if (tempBytes.Length <= maxSizeBytes || desiredQuality < 5)
        //            {
        //                return tempBytes;
        //            }

        //            desiredQuality -= 5;
        //            image.Quality = desiredQuality;
        //        }
        //    }
        //    catch (Exception e)
        //    {
        //        throw new Exception(e.Message);
        //    }
        //}

        public static byte[] ConverterSkia(byte[] imageBytes, int maxquality = 90)
        {
            if (imageBytes.Length > 500 * 1024)
            {
                var stream = new MemoryStream(imageBytes);
                using var skData = SKData.Create(stream);
                using var codec = SKCodec.Create(skData);

                var supportedScale = codec.GetScaledDimensions((float)Width / codec.Info.Width);

                var nearest = new SKImageInfo(supportedScale.Width, supportedScale.Height);
                using var destinationImage = SKBitmap.Decode(codec, nearest);
                using var resizedImage = destinationImage.Resize(new SKImageInfo(Width, Height), SKFilterQuality.High);

                var format = SKEncodedImageFormat.Png;
                using var outputImage = SKImage.FromBitmap(resizedImage);
                using var data = outputImage.Encode(format, maxquality);
                using var outputStream = GetOutputStream("skiasharp");
                data.SaveTo(outputStream);

                using var ms = new MemoryStream();
                outputStream.CopyTo(ms);
                var bytes = ms.ToArray();
                outputStream.Close();
                stream.Close();
                return bytes;
            }
            return imageBytes;
        }

        private static Stream GetOutputStream(string name)
        {
            return File.Open($"images/output_{name}.png", FileMode.OpenOrCreate);
        }

        private static Stream GetStream()
        {
            return File.OpenRead("images/input.jpeg");
        }
    }
}