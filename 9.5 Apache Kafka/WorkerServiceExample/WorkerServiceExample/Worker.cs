using Confluent.Kafka;

namespace WorkerServiceExample
{
    public class Worker(ILogger<Worker> logger) : BackgroundService
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var config = new ProducerConfig
            {
                BootstrapServers = "localhost:9092"
            };

            using var producer = new ProducerBuilder<string, string>(config).Build();

            var invoiceEvent = new
            {
                InvoiceId = Guid.NewGuid(),
                Amount = new Random().Next(100, 1000),
                Supplier = "Amazon"
            };

            var message = new Message<string, string>
                {
                    Key = invoiceEvent.InvoiceId.ToString(),
                    Value = System.Text.Json.JsonSerializer.Serialize(invoiceEvent)
                };

            await producer.ProduceAsync("invoice-created21", message, stoppingToken);

            var consumerConfig = new ConsumerConfig
            {
                BootstrapServers = "localhost:9092",
                GroupId = "worker-service-group",
                AutoOffsetReset = AutoOffsetReset.Earliest
            };

            var consumer = new ConsumerBuilder<string, string>(consumerConfig).Build();
            consumer.Subscribe("invoice-created");

            while (!stoppingToken.IsCancellationRequested)
            {
               var result = consumer.Consume(stoppingToken);
                if (result != null)
                {
                    logger.LogInformation("Received message: {Message}", result.Message.Value);
                }




                await Task.Delay(1000, stoppingToken);
            }
        }


    }
}
