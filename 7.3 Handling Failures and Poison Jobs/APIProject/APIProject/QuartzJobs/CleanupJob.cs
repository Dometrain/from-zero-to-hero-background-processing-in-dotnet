using Quartz;

namespace APIProject.QuartzJobs
{
    [DisallowConcurrentExecution]
    public class CleanupJob : IJob
    {
        private ILogger<CleanupJob> _logger;

        public CleanupJob(ILogger<CleanupJob> logger)
        {
            _logger = logger;
        }

        public Task Execute(IJobExecutionContext context)
        {
            _logger.LogInformation("Executing cleanup job at {Time}", DateTimeOffset.Now);

            return Task.CompletedTask;
        }
    }
}
