using Google.Api;

using risk.control.system.Models;
using risk.control.system.Models.ViewModel;

namespace risk.control.system.Helpers
{
    public static class MediaIdfyHelper
    {
        public static void UpdateMediaMetadata(MediaReport media, string path, string name, string lat, string lon)
        {
            media.FilePath = path;
            media.ImageExtension = Path.GetExtension(name);
            media.MediaExtension = media.ImageExtension.TrimStart('.');
            media.LongLat = $"Latitude = {lat}, Longitude = {lon}";
            media.LongLatTime = DateTime.UtcNow;
            media.ValidationExecuted = true;
            media.ImageValid = true;
        }

        public static void DetermineMediaType(MediaReport media, string contentType)
        {
            string[] videoExtensions = { ".mp4", ".webm", ".avi", ".mov", ".mkv" };
            bool isVideo = contentType.ToLower().StartsWith("video/") ||
                           videoExtensions.Contains(media.ImageExtension.ToLower());

            media.MediaType = isVideo ? MediaType.VIDEO : MediaType.AUDIO;
        }
    }
}
