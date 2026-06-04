namespace APIProject.Models
{
    public class BackgroundJobTransition
    {
        public string Id { get; set; }
        public string JobId { get; set; }
        public string FromStatus { get; set; } = string.Empty;
        public string ToStatus { get; set; } = string.Empty;
        public DateTimeOffset TransitionedAt { get; set; }
        public string? Reason { get; set; }
        public string Payload { get; set; } = string.Empty;
    }
}
