using APIProject.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace APIProject.Services
{
    public class DocumentProcessorService
    {
        private readonly AppDbContext _dbContext;
        private readonly ILogger _logger;
        private readonly NotificationService _notificationService;
        public DocumentProcessorService(AppDbContext dbContext, 
            ILogger<DocumentProcessorService> logger,
            NotificationService notificationServcie)
        {
            _dbContext = dbContext;
            _logger = logger;
            _notificationService = notificationServcie;
        }

        public async Task ProcessCompleteDocumentNotification(Document document, 
            string email)
        {
            var documentJob = await _dbContext.BackgroundJobs.SingleAsync
                (j => j.Id == document.Id);
            
            if(documentJob.Status == "Completed")
            {
                return;
            }

            var notificationExists = await _dbContext.EmailNotifications
                .AnyAsync(n => n.DocumentId == document.Id
                && n.Type == "ProcessingComplete" 
                && n.Sent);

            if (notificationExists == false)
            {
                _dbContext.EmailNotifications.Add(new EmailNotification()
                {
                    Id = Guid.NewGuid(),
                    DocumentId = document.Id,
                    Type = "ProcessingComplete",
                    Recipient = email
                });
            }

            documentJob.Status = "Completed";
            documentJob.CompletedAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync();
        }
            
        

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
