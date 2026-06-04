namespace APIProject.Models
{
    public class BackgroundJob
    {
        public Guid Id { get; set; }
        public string Type { get; set; } = string.Empty;
        public string Payload { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
       
    }

    
}
