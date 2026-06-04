using APIProject.Interfaces;
using APIProject.QuartzJobs;
using APIProject.Services;
using APIProject.TickerQJobs;
using Hangfire;
using Hangfire.Storage.SQLite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Quartz;
using TickerQ.Dashboard.DependencyInjection;
using TickerQ.DependencyInjection;
using TickerQ.Utilities;
using TickerQ.Utilities.Entities;
using TickerQ.Utilities.Interfaces.Managers;

namespace APIProject
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddTickerQ(tickerOptions =>
            {
                tickerOptions.AddDashboard();
            });

            // Add services to the container.

            builder.Services.AddControllers();

            builder.Services.AddQuartz(quartz =>
            {
               var jobKey = new JobKey("CleanupJob");
                quartz.AddJob<CleanupJob>(jobOptions =>
                {
                    jobOptions.WithIdentity(jobKey);
                });

                quartz.AddTrigger(triggerOptions =>
                {
                    triggerOptions.
                            ForJob(jobKey)
                            .WithIdentity("CleanupJob-Trigger")
                            .WithSimpleSchedule(scheduleOptions =>
                                scheduleOptions.WithIntervalInSeconds(10)
                                    .RepeatForever());
                });
            });

            builder.Services.AddQuartzHostedService(hostedServiceOptions =>
            {
                hostedServiceOptions.WaitForJobsToComplete = true;
            });

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

            //builder.Services.AddTransient<DocumentService>();
            //builder.Services.AddTransient<NotificationService>();
            //builder.Services.AddTransient<DocumentProcessorService>();
            //builder.Services.AddSingleton<IBackgroundJobQueue, BackgroundJobQueue>();
            //builder.Services.AddHostedService<QueuedJobWorker>();
            //builder.Services.AddScoped<DocumentCleanUpService>();
            //builder.Services.AddHostedService<CleanupWorker>();

            builder.Services.AddDbContext<AppDbContext>(options =>
                options.UseSqlite(
                    builder.Configuration.GetConnectionString("Default")
                    ));

            var app = builder.Build();

            app.UseTickerQ();

            // Configure the HTTP request pipeline.
            app.UseHangfireDashboard("/hangfire");

            RecurringJob.AddOrUpdate<ReportService>("daily-report", reportService => reportService.GenerateDailyReportAsync(), Cron.Minutely);
            
            app.UseHttpsRedirection();
            app.UseAuthorization();
            app.MapControllers();

            app.MapPost("/reports/{reportId:guid}/schedule",
                async (ITimeTickerManager<TimeTickerEntity> timeTicker,
                       CancellationToken cancellation, Guid reportId) =>
                    {
                        var result = await timeTicker.AddAsync(
                                new TimeTickerEntity
                                {
                                    Function = "GenerateDailyReport",
                                    ExecutionTime = DateTime.UtcNow.AddSeconds(5), // Schedule to run 1 minute from now
                                    Request = TickerHelper.CreateTickerRequest(new ReportRequest(reportId))
                                },
                                cancellation
                            );

                        return result.IsSucceeded ? 
                            Results.Accepted($"/reports/jobs/{result.Result.Id}") 
                            : Results.BadRequest("Failed to schedule report generation");
                    }
            );




            app.Run();
        }
    }
}
