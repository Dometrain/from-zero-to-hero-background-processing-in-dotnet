using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Text.Json;

namespace EmailNotificationService
{
    public class Worker(ILogger<Worker> logger) : BackgroundService
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var factory = new ConnectionFactory()
            {
                HostName = "localhost"
            };


            using var connection = await factory.CreateConnectionAsync();
            using var channel = await connection.CreateChannelAsync();


            var consumer = new AsyncEventingBasicConsumer(channel);

            consumer.ReceivedAsync += (model, args) =>
            {
                var body = args.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                var job = JsonSerializer.Deserialize<BackgroundJob>(message);
                SendEmail(job);
                return Task.CompletedTask;
            };

            while (!stoppingToken.IsCancellationRequested)
            {
                if (logger.IsEnabled(LogLevel.Information))
                {



                    await channel.BasicConsumeAsync(
                        queue: "order-updates",
                        autoAck: true,
                        consumer: consumer
                        );



                }
            }
            await Task.Delay(1000, stoppingToken);
        }

        private void SendEmail(BackgroundJob job)
        {
            //send smtp email
            using var client = new SmtpClient("smtp.gmail.com")
            {
                Port = 587,
                Credentials = new NetworkCredential("nickpproud@gmail.com", "uqcq kcup awea nsec"),
                EnableSsl = true,
                
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress("nickpproud@gmail.com"),
                To = { new MailAddress("nickpproud@gmail.com") },
                Subject = "Order Status Update - " + job.Type,
                Body = $"Your order with ID {job.Id} has been updated to status {job.Type}"
            };
            try
            {
                client.Send(mailMessage);

            }
            catch (Exception ex)
            {
                //log error
                Console.WriteLine($"Failed to send email for job {job.Id}: {ex.Message}");

            }
        }
    }
}
