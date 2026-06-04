using APIProject.Models;

namespace APIProject.Services
{
    public class DocumentProcessorService
    {
        public async Task<Document> ProcessAsync(Guid id, CancellationToken cancellation)
        {
  
               return await Task.FromResult(new Document()
                {
                    Id = id,
                    Name = "foo",
                    Content = null,
                });
        }

        public async Task<Guid> CreateDocProcessingJob(Guid id, CancellationToken cancellation)
        {
            throw new NotImplementedException();
        }
    }
}
