using Microsoft.Extensions.Hosting;

namespace APIProject
{
    public class JobProcessor : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;

        public JobProcessor(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                Console.WriteLine("Checking for jobs...");

                using var scope = _scopeFactory.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                await ProcessJob(dbContext);
                await Task.Delay(1000, stoppingToken);
            }
        }

        private static async Task ProcessJob(AppDbContext dbContext)
        {
            Console.WriteLine("Processing a job...");
            await Task.Delay(500);
        }
    }
}
