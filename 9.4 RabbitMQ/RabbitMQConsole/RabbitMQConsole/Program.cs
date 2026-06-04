using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;

namespace RabbitMQConsole
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            var factory = new ConnectionFactory()
            {
                HostName = "localhost"
            };

            using var connection = await factory.CreateConnectionAsync();
            using var channel = await connection.CreateChannelAsync();

            await channel.QueueDeclareAsync(
                queue: "invoice-jobs",
                durable: false,
                exclusive: false,
                autoDelete: false);

            var message = "Generate invoice 12345";

            var body = Encoding.UTF8.GetBytes(message);

            await channel.BasicPublishAsync(
                exchange: "",
                routingKey: "invoice-jobs",
                basicProperties: new BasicProperties(),
                body: body,
                mandatory: true);

            var consumer = new AsyncEventingBasicConsumer(channel);

            consumer.ReceivedAsync += (model, args) =>
            {
                var body = args.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                return Task.CompletedTask;
            };

            await channel.BasicConsumeAsync(
                queue: "invoice-jobs",
                autoAck: true,
                consumer: consumer
                );

        }
    }
}
