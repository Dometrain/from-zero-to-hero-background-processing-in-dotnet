namespace APIProject.Services
{
    public sealed class ReportService
    {
        private readonly ILogger<ReportService> _logger;

        public ReportService(
            ILogger<ReportService> logger)
        {
            _logger = logger;
        }

        public Task GenerateDailyReportAsync()
        {
            _logger.LogInformation(
                "Generating daily report at {Time}",
                DateTimeOffset.UtcNow);

            return Task.CompletedTask;
        }
    }
}
