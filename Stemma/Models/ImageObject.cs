namespace Stemma.Models
{
    public class ImageObject
    {
        public Google.Apis.Storage.v1.Data.Object? imageObject;

        public byte[] imageInByte { get; set; } = new byte[0];
        public string imageInSvg { get; set; } = string.Empty;

        public string folderName { get; set; } = string.Empty;
        public string imageName { get; set; } = string.Empty;
        public string imageExtension { get; set; } = string.Empty;
    }
}
