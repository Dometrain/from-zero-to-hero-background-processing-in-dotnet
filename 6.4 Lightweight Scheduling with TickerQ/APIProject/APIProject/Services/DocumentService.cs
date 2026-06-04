using APIProject.Models;

namespace APIProject.Services
{
    public class DocumentService
    {
        public async Task<Document> SaveAsync(DocumentUpload upload, CancellationToken cancellationToken)
        {
            return await Task.FromResult(new Document()
            {
                Id = Guid.NewGuid(),
                Name = "foo",
                Content = null,
            });
        }

        public async Task SaveResultAsync(Guid id, Document document, CancellationToken cancellation)
        {
            throw new NotImplementedException();
        }
    }
}
