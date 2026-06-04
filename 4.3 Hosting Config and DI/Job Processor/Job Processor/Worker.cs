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
                //example critical logging
                logger.LogCritical("WARNING CRITICAL ERROR!");
                using var scope = serviceScopeFactory.CreateScope();
                var jobProcessor = scope.ServiceProvider.GetRequiredService<JobProcessor>();

                await jobProcessor.ProcessNextJob();
                await Task.Delay(_loopInterval, stoppingToken);
            }
        }
    }
}
