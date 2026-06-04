using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace Job_Processor
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = Host.CreateApplicationBuilder(args);
            builder.Services.AddHostedService<Worker>();
            builder.Services.AddScoped<JobProcessor>();
            builder.Services.AddWindowsService();
            var host = builder.Build();
            host.Run();
        }
    }
}
