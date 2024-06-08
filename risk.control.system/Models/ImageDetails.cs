namespace risk.control.system.Models
{
    public class ImageDetails
    {
        public int Id { get; set; }
        public string FileName { get; set; }
        public string Hash { get; set; }
        public byte[] ImageData { get; set; }
    }
}
