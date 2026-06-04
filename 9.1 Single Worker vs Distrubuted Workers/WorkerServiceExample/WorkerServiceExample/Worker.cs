namespace WorkerServiceExample
{
    public class Worker(ILogger<Worker> logger) : BackgroundService
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var job = await GetJobAsync(stoppingToken);
                if (job != null)
                {
                    await ProcessJobAsync(job, stoppingToken);
                }
                await Task.Delay(1000, stoppingToken);
            }
        }


        private async Task<BackgroundJob> GetJobAsync(CancellationToken cancellationToken)
        {
            // Simulate getting a job
            await Task.Delay(500, cancellationToken);
            if (logger.IsEnabled(LogLevel.Debug))
            {
                logger.LogDebug("{time}: Job Retrieved", DateTimeOffset.Now);
            }
            return new BackgroundJob();
        }

        private async Task ProcessJobAsync(BackgroundJob job, CancellationToken cancellationToken)
        {
            // Simulate processing a job
            await Task.Delay(1000, cancellationToken);
            if (logger.IsEnabled(LogLevel.Debug))
            {
                logger.LogDebug("{time}: Job Processed", DateTimeOffset.Now);
            }
        }
    }
}
