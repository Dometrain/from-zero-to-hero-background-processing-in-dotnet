namespace APIProject.Services
{
    public sealed class CleanupWorker : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<CleanupWorker> _logger;

        public CleanupWorker(
            IServiceScopeFactory scopeFactory,
            ILogger<CleanupWorker> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(
            CancellationToken stoppingToken)
        {
            using var timer = new PeriodicTimer(TimeSpan.FromSeconds(5));

            while(await timer.WaitForNextTickAsync(stoppingToken))
            {
                try
                {
                   await RunCleanUpAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during cleanup");
                }
            }
        }

        private async Task RunCleanUpAsync(CancellationToken cancellationToken)
        {
            using var scope = _scopeFactory.CreateScope();
            var documentCleanUpService = scope.ServiceProvider.GetService<DocumentCleanUpService>();
            await documentCleanUpService.DeleteExpiredDocumentsAsync(cancellationToken);
        }

        
    }
}
