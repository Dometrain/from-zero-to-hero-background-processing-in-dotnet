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

        private async Task StartDailyRun()
        {
            //DON'T DO THIS!!!
            try
            {
                await GenerateDailyReportAsync();
            }
            catch
            {
                await GenerateDailyReportAsync();
            }
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
