namespace APIProject.Models
{
    public class Document
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public byte[] Content { get; set; }
    }
}
