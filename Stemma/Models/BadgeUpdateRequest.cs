namespace Stemma.Models
{
    public class BadgeUpdateRequest
    {
        public string UserId { get; set; } = string.Empty;
        public string OldName { get; set; } = string.Empty;
        public string NewName { get; set; } = string.Empty;
    }
}
