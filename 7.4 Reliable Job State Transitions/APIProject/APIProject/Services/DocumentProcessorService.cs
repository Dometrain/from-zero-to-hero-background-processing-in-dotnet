using APIProject.Models;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.EntityFrameworkCore;
using Polly;
using System.Text.Json;

namespace APIProject.Services
{
    public class DocumentProcessorService
    {
        private readonly AppDbContext _dbContext;
        private readonly ILogger _logger;
        private readonly NotificationService _notificationService;
        private ResiliencePipeline _docApiRetryPipeline;
        private readonly HttpClient _httpClient;
        public DocumentProcessorService(AppDbContext dbContext, 
            ILogger<DocumentProcessorService> logger,
            NotificationService notificationService,
            IHttpClientFactory httpClientFactory)
        {
            _dbContext = dbContext;
            _logger = logger;
            _notificationService = notificationService;
            _httpClient = httpClientFactory.CreateClient("DocumentProcessorClient");
            CreateDocAPIRetryPipeline();
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

        private void CreateDocAPIRetryPipeline()
        {
            _docApiRetryPipeline = new ResiliencePipelineBuilder()
                .AddRetry(new Polly.Retry.RetryStrategyOptions()
                {
                    MaxRetryAttempts = 3,
                    Delay = TimeSpan.FromSeconds(5),
                    BackoffType = DelayBackoffType.Exponential,
                    UseJitter = true,
                    ShouldHandle = new PredicateBuilder()
                        .Handle<HttpRequestException>()
                        .Handle<TimeoutException>(),
                    OnRetry = args =>
                    {
                        _logger.LogWarning("Retrying API call due to transient error. Attempt {RetryAttempt}.", args.AttemptNumber);

                        return default;
                    }
                }).Build();
         
        }

        public async Task PostDocumentIdToAPI(Guid docId, CancellationToken cancellationToken)
        {
            await _docApiRetryPipeline.ExecuteAsync(
                    async token =>
                    {
                        var response = _httpClient.PostAsJsonAsync("/api/documents/process",
                            new { DocumentId = docId }, token);
                    },
                    cancellationToken
                );
        }
    }
}
