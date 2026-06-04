using APIProject.Interfaces;
using APIProject.Services;
using Hangfire;
using Hangfire.Storage.SQLite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace APIProject
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.AddControllers();

            builder.Services.AddHangfire(config =>
            {
                var sqlitePath = builder.Configuration.GetConnectionString("Hangfire");
                config
                    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
                    .UseSimpleAssemblyNameTypeSerializer()
                    .UseRecommendedSerializerSettings()
                    .UseSQLiteStorage(sqlitePath);
            });

            builder.Services.AddHangfireServer();

            builder.Services.AddTransient<DocumentService>();
            builder.Services.AddTransient<NotificationService>();
            builder.Services.AddTransient<DocumentProcessorService>();
            builder.Services.AddSingleton<IBackgroundJobQueue, BackgroundJobQueue>();
            builder.Services.AddHostedService<QueuedJobWorker>();
            builder.Services.AddScoped<DocumentCleanUpService>();
            builder.Services.AddHostedService<CleanupWorker>();

            builder.Services.AddDbContext<AppDbContext>(options =>
                options.UseSqlite(
                    builder.Configuration.GetConnectionString("Default")
                    ));

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            app.UseHangfireDashboard("/hangfire");

            RecurringJob.AddOrUpdate<ReportService>("daily-report", reportService => reportService.GenerateDailyReportAsync(), Cron.Minutely);
            
            app.UseHttpsRedirection();
            app.UseAuthorization();
            app.MapControllers();

            app.Run();
        }
    }
}
