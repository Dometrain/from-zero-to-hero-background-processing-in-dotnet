
using APIProject.Interfaces;
using APIProject.Models;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace APIProject.Services
{
    public class QueuedJobWorker : BackgroundService
    {
       
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<QueuedJobWorker> _logger;

        public QueuedJobWorker(IBackgroundJobQueue jobQueue, 
            IServiceScopeFactory scopeFactory, 
            ILogger<QueuedJobWorker> logger)
        {
          
            _scopeFactory = scopeFactory;
            _logger = logger;
            
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
                catch(Exception ex)
                {
                    
                    _logger.LogError(ex, "Error processing job {JobId} of type {JobType}", 
                        job?.Id, job?.Type);
                }
            }
        }


        private async Task ProcessJobAsync(BackgroundJob job, CancellationToken cancellationToken)
        {
            if(job == null)
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
            catch
            {
                await SetJobToFailed(job, cancellationToken, db);
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

        private async Task SetJobToFailed(BackgroundJob job, CancellationToken cancellationToken, AppDbContext db)
        {
            job.Status = "Failed";
            db.BackgroundJobs.Update(job);
            await db.SaveChangesAsync();
        }
    }
}
