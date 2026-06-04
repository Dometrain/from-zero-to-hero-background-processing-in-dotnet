using Microsoft.Extensions.DependencyInjection;

namespace Job_Processor
{
    
    
    
    public class Worker(ILogger<Worker> logger, IServiceScopeFactory serviceScopeFactory) : BackgroundService
    {
       
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                logger.LogInformation("Processing next job at: {time}", DateTimeOffset.Now);
                using var scope = serviceScopeFactory.CreateScope();
                var jobProcessor = scope.ServiceProvider.GetRequiredService<JobProcessor>();

                await jobProcessor.ProcessNextJob();
                await Task.Delay(1000, stoppingToken);
            }
        }
    }
}
