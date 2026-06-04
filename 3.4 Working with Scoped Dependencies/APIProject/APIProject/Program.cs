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
            builder.Services.AddHostedService<JobProcessor>();
            builder.Services.AddDbContext<AppDbContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("Default")
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
