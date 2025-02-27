namespace Stemma.Models
{
    public class BadgeUploadRequest
    {
        public IFormFile BadgeFile { get; set; }
        public string BadgeName { get; set; }
        public string UserId { get; set; }
    }
}
