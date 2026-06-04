using System;
using System.Collections.Generic;
using System.Text;

namespace _3._2_Background_Service_vs_IHostedService
{
    public class JobProcessor : BackgroundService
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                Console.WriteLine("Checking for jobs....");
                await Task.Delay(1000, stoppingToken);
            }
        }
    }
}
