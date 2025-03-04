namespace Stemma.Models
{
    public class BadgeUploadRequest
    {
        public IFormFile BadgeFile { get; set; } = null!;
        public string BadgeName { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
    }
}
