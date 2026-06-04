
using APIProject.Interfaces;
using APIProject.Models;
using System.Text.Json;

namespace APIProject.Services
{
    public class QueuedJobWorker : BackgroundService
    {
        private readonly IBackgroundJobQueue _jobQueue;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<QueuedJobWorker> _logger;

        public QueuedJobWorker(IBackgroundJobQueue jobQueue, 
            IServiceScopeFactory scopeFactory, 
            ILogger<QueuedJobWorker> logger)
        {
            _jobQueue = jobQueue;
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected async override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var job = await _jobQueue.DequeueAsync(stoppingToken);

                try
                {
                    await ProcessJobAsync(job, stoppingToken);
                }
                catch(Exception ex)
                {
                    _logger.LogError(ex, "Error processing job {JobId} of type {JobType}", 
                        job?.Id, job?.Type);
                }
            }
        }


        private async Task ProcessJobAsync(BackgroundJob job, CancellationToken cancellationToken)
        {
            using var scope = _scopeFactory.CreateScope();

            switch (job.Type)
            {
                case "ProcessDocument":
                    var payload = JsonSerializer.Deserialize<ProcessDocumentPayload>(job.Payload);
                    var documentProcessor = scope.ServiceProvider.GetService<DocumentProcessorService>();
                    await documentProcessor.ProcessAsync(payload.DocumentId, cancellationToken);
                    break;
                default:
                    throw new InvalidOperationException($"Unknown job type: {job.Type}");
            }
        }
    }
}
