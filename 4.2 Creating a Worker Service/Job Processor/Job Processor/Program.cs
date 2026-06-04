namespace Job_Processor
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = Host.CreateApplicationBuilder(args);
            builder.Services.AddHostedService<Worker>();
            builder.Services.AddScoped<JobProcessor>();
            var host = builder.Build();
            host.Run();
        }
    }
}
