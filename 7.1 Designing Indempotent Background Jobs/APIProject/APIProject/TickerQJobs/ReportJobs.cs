using TickerQ.Utilities.Base;

namespace APIProject.TickerQJobs
{
    public class ReportJobs
    {
        private readonly ILogger<ReportJobs> _logger;

        public ReportJobs(ILogger<ReportJobs> logger)
        {
            _logger = logger;
        }

        [TickerFunction("GenerateDailyReport")] 
        public Task GenerateDailyReport(TickerFunctionContext<ReportRequest> context, 
            CancellationToken cancellation)
        {
            var request = context.Request;
            
            _logger.LogInformation("Request {RequestId} Recieved: Generating daily report at {Time}", request.RequestId, DateTimeOffset.Now);
            // Simulate report generation work
            return Task.CompletedTask;
        }
    }

    public record ReportRequest(Guid RequestId);
}
