namespace APIProject.Models
{
    public class EmailNotification
    {
        public Guid Id { get; set; }
        public Guid DocumentId { get; set; }
        public string Type { get; set; }
        public string Recipient { get; set; }
        public bool Sent { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? SentAt { get; set; }
    }
}
