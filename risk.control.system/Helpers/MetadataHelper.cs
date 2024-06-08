
using MetadataExtractor;

namespace risk.control.system.Helpers
{

    public static class MetadataHelper
    {
        public static Dictionary<string, string> ExtractMetadata(byte[] imageBytes)
        {
            using (var ms = new MemoryStream(imageBytes))
            {
                var directories = ImageMetadataReader.ReadMetadata(ms);
                var metadata = new Dictionary<string, string>();

                foreach (var directory in directories)
                {
                    foreach (var tag in directory.Tags)
                    {
                        metadata[tag.Name] = tag.Description;
                    }
                }

                return metadata;
            }
        }

        public static bool CompareMetadata(Dictionary<string, string> originalMetadata, Dictionary<string, string> newMetadata)
        {
            foreach (var key in originalMetadata.Keys)
            {
                if (!newMetadata.ContainsKey(key) || originalMetadata[key] != newMetadata[key])
                {
                    return false;
                }
            }

            return true;
        }
    }

}
