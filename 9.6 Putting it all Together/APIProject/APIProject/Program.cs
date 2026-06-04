using APIProject.HealthChecks;
using APIProject.Interfaces;
using APIProject.Models;
using APIProject.QuartzJobs;
using APIProject.Services;
using APIProject.TickerQJobs;
using Hangfire;
using Hangfire.Storage.SQLite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Quartz;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;
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

            builder.Services.AddTransient<DocumentService>();
            builder.Services.AddTransient<NotificationService>();
            builder.Services.AddTransient<DocumentProcessorService>();
            builder.Services.AddSingleton<IBackgroundJobQueue, BackgroundJobQueue>();
            builder.Services.AddSingleton<WorkerHeartbeatService>();
            builder.Services.AddHostedService<QueuedJobWorker>();
            builder.Services.AddScoped<DocumentCleanUpService>();
            builder.Services.AddHostedService<CleanupWorker>();
            builder.Services.AddHttpClient();
            builder.Services.AddDbContext<AppDbContext>(options =>
                options.UseSqlite(
                    builder.Configuration.GetConnectionString("Default")
                    ));


            builder.Services.AddHealthChecks()
                .AddDbContextCheck<AppDbContext>()
                .AddCheck<WorkerHeartBeatHealthCheck>("Heartbeat_Check");
            var app = builder.Build();

            app.UseTickerQ();

            app.MapHealthChecks("/health");

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

            app.MapPost("/order-update/{status}",
                async (
                CancellationToken cancellationToken, 
                string status) =>
                {
                    var job = new APIProject.Models.BackgroundJob()
                    {
                        Id = Guid.NewGuid().ToString(),
                        Type = status,
                        CreatedAt = DateTime.UtcNow
                    };

                    var factory = new ConnectionFactory()
                    {
                        HostName = "localhost"
                    };

                    using var connection = await factory.CreateConnectionAsync();
                    using var channel = await connection.CreateChannelAsync();

                    await channel.QueueDeclareAsync(
                        queue: "order-updates",
                        durable: false,
                        exclusive: false,
                        autoDelete: false);

                    var message = JsonSerializer.Serialize(job);
                    var body = Encoding.UTF8.GetBytes(message);

                    await channel.BasicPublishAsync(
                            exchange: "",
                            routingKey: "order-updates",
                            basicProperties: new BasicProperties(),
                            body: body,
                            mandatory: true
                        );


                    return Results.Accepted($"/background-jobs/{job.Id}");
                });
            


            app.MapGet("/background-jobs/metrics",
                async (AppDbContext db, CancellationToken cancellationToken) =>
                {
                    var now = DateTimeOffset.UtcNow;

                    var pendingJobs = await db.BackgroundJobs
                        .CountAsync(x => x.Status == "Pending", cancellationToken);

                    var processingJobs = await db.BackgroundJobs
                        .CountAsync(x => x.Status == "Processing", cancellationToken);

                    var completedJobs = await db.BackgroundJobs
                        .CountAsync(x => x.Status == "Completed", cancellationToken);

                    var deadLetterJobs = await db.BackgroundJobs
                        .CountAsync(x => x.Status == "DeadLetter", cancellationToken);

                    var oldestPendingJobCreatedAt = await db.BackgroundJobs
                        .Where(x => x.Status == JobStatuses.Pending)
                        .OrderBy(x => x.CreatedAt)
                        .Select(x => (DateTimeOffset?)x.CreatedAt)
                        .FirstOrDefaultAsync(cancellationToken);

                    var oldestPendingJobAgeSeconds = oldestPendingJobCreatedAt is null
                        ? 0
                        : (now - oldestPendingJobCreatedAt.Value).TotalSeconds;

                    return Results.Ok(new
                    {
                        PendingJobs = pendingJobs,
                        ProcessingJobs = processingJobs,
                        CompletedJobs = completedJobs,
                        DeadLetterJobs = deadLetterJobs,
                        OldestPendingJobAgeSeconds = oldestPendingJobAgeSeconds
                    });
            });


            app.Run();
        }
    }
}
