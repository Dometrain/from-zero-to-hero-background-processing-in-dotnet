namespace APIProject.Services
{
    public class DocumentCleanUpService
    {
        //for demonstration purposes, this service will just log the cleanup action
        private readonly ILogger<DocumentCleanUpService> _logger;

        public DocumentCleanUpService(ILogger<DocumentCleanUpService> logger)
        {
            _logger = logger;
        }

        public async Task DeleteExpiredDocumentsAsync(CancellationToken cancellationToken)
        {
            // In a real implementation, this method would query the database for documents
            // that have expired and delete them. For demonstration, we'll just log the action.
            _logger.LogInformation("Starting cleanup of expired documents at {Time}", DateTime.UtcNow);
            // Simulate some work
            await Task.Delay(1000, cancellationToken);
            _logger.LogInformation("Completed cleanup of expired documents at {Time}", DateTime.UtcNow);
        }
    }
}
