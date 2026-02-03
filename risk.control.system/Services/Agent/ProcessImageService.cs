using risk.control.system.Helpers;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Processing;

namespace risk.control.system.Services.Agent
{
    public interface IProcessImageService
    {
        byte[] ProcessCompress(byte[] imageByte, string onlyExtension, float cornerRadius = 10, int quality = 99, Amazon.Rekognition.Model.BoundingBox? faceBox = null);
    }

    public class ProcessImageService : IProcessImageService
    {
        public byte[] ProcessCompress(byte[] imageByte, string onlyExtension, float cornerRadius = 10, int quality = 99, Amazon.Rekognition.Model.BoundingBox? faceBox = null)
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
    }
}