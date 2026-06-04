using APIProject.Interfaces;
using APIProject.Services;
using Microsoft.EntityFrameworkCore;

namespace APIProject
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.AddControllers();
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
            app.UseHttpsRedirection();
            app.UseAuthorization();
            app.MapControllers();

            app.Run();
        }
    }
}
