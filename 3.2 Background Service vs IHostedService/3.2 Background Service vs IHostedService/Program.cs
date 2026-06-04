namespace _3._2_Background_Service_vs_IHostedService
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = Host.CreateApplicationBuilder(args);
            builder.Services.AddHostedService<Worker>();
            builder.Services.AddHostedService<JobProcessor>();
            var host = builder.Build();
            host.Run();
        }
    }
}
