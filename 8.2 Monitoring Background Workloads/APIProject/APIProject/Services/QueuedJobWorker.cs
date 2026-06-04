using APIProject.Interfaces;
using APIProject.Models;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using System.Net.Http;

namespace APIProject.Services
{
    public class QueuedJobWorker : BackgroundService
    {
        private readonly IBackgroundJobQueue _jobQueue;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<QueuedJobWorker> _logger;
        private readonly HttpClient _httpClient;

        public QueuedJobWorker(
            IBackgroundJobQueue jobQueue,
            IServiceScopeFactory scopeFactory,
            ILogger<QueuedJobWorker> logger,
            IHttpClientFactory httpClientFactory)
        {
            _jobQueue = jobQueue;
            _scopeFactory = scopeFactory;
            _logger = logger;
            _httpClient = httpClientFactory.CreateClient("QueuedJobWorkerClient");
        }

        protected async override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetService<AppDbContext>();

                var job = await db.BackgroundJobs.Where(x => x.Status == "Pending")
                    .OrderByDescending(x => x.CreatedAt)
                    .FirstOrDefaultAsync(stoppingToken);

                try
                {
                    await ProcessJobAsync(job, stoppingToken);
                    await Task.Delay(3000);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing job {JobId} of type {JobType}",
                        job?.Id, job?.Type);
                }
            }
        }

        public async Task<bool> TryTransitionAsync(string jobId, string expectedStatus, 
            string nextStatus, CancellationToken cancellationToken)
        {
            var allowed = allowedTransitions[expectedStatus].Contains(nextStatus);
            if (allowed == false)
            {
                throw new InvalidOperationException($"Invalid status transition from {expectedStatus} to {nextStatus}");
            }

            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetService<AppDbContext>();
            var affectedRows = await db.BackgroundJobs.Where(x => x.Id == jobId.ToString()
            && x.Status == expectedStatus)
                .ExecuteUpdateAsync(x => 
                x.SetProperty(p => p.Status, nextStatus), cancellationToken);

            if(affectedRows == 1)
            {
                var jobTransition = new BackgroundJobTransition
                {
                    Id = Guid.NewGuid().ToString(),
                    JobId = jobId,
                    FromStatus = expectedStatus,
                    ToStatus = nextStatus,
                    TransitionedAt = DateTime.UtcNow
                };

                db.BackgroundJobTransitions.Add(jobTransition);
                await db.SaveChangesAsync(cancellationToken);
            }

            return affectedRows == 1;
        }

        public async Task TransitionAsync(BackgroundJob job, 
            string newStatus,
            CancellationToken cancellationToken)
        {
            var allowed = allowedTransitions[job.Status].Contains(newStatus);
            if (allowed == false)
            {  
                throw new InvalidOperationException($"Invalid status transition from {job.Status} to {newStatus}");
            }

            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetService<AppDbContext>();
            job.Status = newStatus;
            db.BackgroundJobs.Update(job);
            await db.SaveChangesAsync(cancellationToken);
        }

        private static TimeSpan CalculateBackoff(int attempt)
        {
            return attempt switch
            {
                1 => TimeSpan.FromSeconds(5),
                2 => TimeSpan.FromSeconds(15),
                3 => TimeSpan.FromSeconds(30),
                _ => TimeSpan.FromMinutes(1)
            };
        }

        private async Task ProcessJobAsync(BackgroundJob job, CancellationToken cancellationToken)
        {
            
            _logger.LogInformation("Attempting to process job");

            if (job == null)
            {
                // No pending jobs, wait a bit before checking again
                await Task.Delay(1000, cancellationToken);
                _logger.LogInformation("No pending jobs found, waiting...");
                return;
            }

            using var loggingScope = _logger.BeginScope(
               "JobId: {JobId}, JobType: {JobType}, Attempt: {Attempt}",
               job.Id, job.Type, job.RetryCount + 1);
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetService<AppDbContext>();
            try
            {
                switch (job.Type)
                {
                    case "ProcessDocument":
                        _logger.LogInformation("Processing document job with payload: {Payload}", job.Payload);
                        var payload = JsonSerializer.Deserialize<ProcessDocumentPayload>(job.Payload);
                        var documentProcessor = scope.ServiceProvider.GetService<DocumentProcessorService>();
                        _logger.LogInformation("Attempting to transition job to Processing");
                        var dequeued = await TryTransitionAsync(job.Id, "Pending", "Processing", cancellationToken);
                        if(dequeued == false)
                        {
                            // Another worker has taken this job, skip processing
                            _logger.LogInformation("Failed to transition job to Processing, it may have been taken by another worker. Skipping...");
                            return;
                        }
                        _logger.LogInformation("Job transitioned to Processing, starting document processing");
                        await documentProcessor.ProcessAsync(payload.DocumentId, cancellationToken);
                        _logger.LogInformation("Document processing complete, transitioning job to Complete");
                        await TransitionAsync(job, "Complete", cancellationToken);
                        _logger.LogInformation("Job transitioned to Complete");
                        break;
                    default:
                        throw new InvalidOperationException($"Unknown job type: {job.Type}");
                }
            }
            catch (Exception ex)
            {
                await TransitionAsync(job, "DeadLetter", cancellationToken);
                throw;
            }

        }

        private async Task SetJobToInProgress(BackgroundJob job, CancellationToken cancellationToken, AppDbContext db)
        {
            job.Status = "Processing";
            db.BackgroundJobs.Update(job);
            await db.SaveChangesAsync();
        }

        private async Task SetJobComplete(BackgroundJob job, CancellationToken cancellationToken, AppDbContext db)
        {
            job.Status = "Complete";
            db.BackgroundJobs.Update(job);
            await db.SaveChangesAsync();
        }

        private async Task SetJobToFailed(BackgroundJob job,
            CancellationToken cancellationToken,
            AppDbContext db,
            Exception exception)
        {
            job.RetryCount++;
            job.LastError = exception.Message;
            var maximumRetries = 5;

            if(IsPermanentFailure(exception))
            {
                job.Status = "DeadLetter";
                db.BackgroundJobs.Update(job);
                await db.SaveChangesAsync();
                return;
            }

            if (job.RetryCount <= maximumRetries)
            {
                var backOff = CalculateBackoff(job.RetryCount);
                job.NextAttemptAt = DateTime.UtcNow.Add(backOff);
            }
            else
            {
                job.Status = "Failed";
                db.BackgroundJobs.Update(job);
            }

            await db.SaveChangesAsync();

        }

        public static bool IsPermanentFailure(Exception ex)
        {
            return ex is UnknownJobTypeException
                or PayloadInvalidException;
        }

        private static readonly Dictionary<string, string[]> allowedTransitions = new()
        {
            ["Pending"] = [
                    "Processing",
                    "DeadLetter"
                ],
            ["Processing"] = [
                "Complete",
                "Pending",
                "DeadLetter"
            ],
            ["DeadLetter"] = [
                "Pending"
                ],
            ["Complete"] = [],
        };
    }

    public class UnknownJobTypeException : Exception
    {
        public UnknownJobTypeException(string jobType)
            : base($"Unknown job type: {jobType}")
        {
        }
    }

    public class PayloadInvalidException: Exception
    {
        public PayloadInvalidException(string message)
            : base(message)
        {
        }
    }
}
