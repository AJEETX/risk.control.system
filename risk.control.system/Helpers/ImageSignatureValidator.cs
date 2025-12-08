namespace risk.control.system.Helpers
{
    public static class ImageSignatureValidator
    {
        private static readonly Dictionary<string, byte[][]> _fileSignatures = new()
    {
        { ".jpeg", new[] { new byte[] { 0xFF, 0xD8, 0xFF } } },
        { ".jpg",  new[] { new byte[] { 0xFF, 0xD8, 0xFF } } },
        { ".png",  new[] { new byte[] { 0x89, 0x50, 0x4E, 0x47 } } },
    };

        public static bool HasValidSignature(IFormFile file)
        {
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!_fileSignatures.ContainsKey(ext)) return false;

            var signatures = _fileSignatures[ext];
            using var reader = new BinaryReader(file.OpenReadStream());
            var headerBytes = reader.ReadBytes(signatures.Max(s => s.Length));
            return signatures.Any(sig => headerBytes.Take(sig.Length).SequenceEqual(sig));
        }
    }
}
