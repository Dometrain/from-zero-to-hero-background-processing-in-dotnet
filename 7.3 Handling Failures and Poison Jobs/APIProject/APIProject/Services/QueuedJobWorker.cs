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
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing job {JobId} of type {JobType}",
                        job?.Id, job?.Type);
                }
            }
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
            if (job == null)
            {
                // No pending jobs, wait a bit before checking again
                await Task.Delay(1000, cancellationToken);
                return;
            }
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetService<AppDbContext>();
            try
            {
                switch (job.Type)
                {
                    case "ProcessDocument":
                        var payload = JsonSerializer.Deserialize<ProcessDocumentPayload>(job.Payload);
                        var documentProcessor = scope.ServiceProvider.GetService<DocumentProcessorService>();
                        await SetJobToInProgress(job, cancellationToken, db);
                        await documentProcessor.ProcessAsync(payload.DocumentId, cancellationToken);
                        await SetJobComplete(job, cancellationToken, db);
                        break;
                    default:
                        throw new InvalidOperationException($"Unknown job type: {job.Type}");
                }
            }
            catch (Exception ex)
            {
                await SetJobToFailed(job, cancellationToken, db, ex);
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
