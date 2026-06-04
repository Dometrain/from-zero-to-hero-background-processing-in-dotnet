using Microsoft.Extensions.DependencyInjection;

namespace Job_Processor
{
    
    
    
    public class Worker(ILogger<Worker> logger, 
        IServiceScopeFactory serviceScopeFactory,
        IConfiguration configuration) : BackgroundService
    {
        private int _loopInterval = configuration.GetValue<int>("LoopInterval");

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            
            while (!stoppingToken.IsCancellationRequested)
            {
                logger.LogInformation("Processing next job at: {time}", DateTimeOffset.Now);
                using var scope = serviceScopeFactory.CreateScope();
                var jobProcessor = scope.ServiceProvider.GetRequiredService<JobProcessor>();

                var pendingJobs = await jobProcessor.GetTotalPendingJobs();

                if (pendingJobs > 0)
                {
                    while (pendingJobs > 0)
                    {
                        var job = await jobProcessor.GetNextJob();
                        try
                        {
                            await jobProcessor.MarkJobInProcess(job);
                            await jobProcessor.ProcessNextJob(job);
                            await jobProcessor.MarkJobAsComplete(job);
                        }
                        catch (Exception ex)
                        {
                            logger.LogError(ex, "Error processing job: {job}", job);
                            await jobProcessor.MarkJobAsFailed(job);
                        }
                       
                        pendingJobs--;
                    }
                   
                    
                }

                await Task.Delay(_loopInterval, stoppingToken);
            }
        }
    }
}
