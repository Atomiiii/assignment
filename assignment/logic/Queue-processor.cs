using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

public class PaymentEvent
{
    public string Id { get; set; }
    public decimal Amount { get; set; }
}

public class OrderEvent
{
    public string Id { get; set; }
    public string Product { get; set; }
    public decimal Total { get; set; }
    public string Currency { get; set; }
}

public class QueueProcessor
{
    public async Task ProcessQueueAsync()
    {
        var factory = new ConnectionFactory() { 
            HostName = "rabbitmq",
            UserName = "guest",
            Password = "guest"
         };
        
        using var connection = await factory.CreateConnectionAsync();
        using var channel = await connection.CreateChannelAsync();

        await channel.QueueDeclareAsync("OrderQueue", durable: true, exclusive: false, autoDelete: false, arguments: null);
        await channel.QueueDeclareAsync("PaymentQueue", durable: true, exclusive: false, autoDelete: false, arguments: null);

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += async (model, ea) =>
        {
            var body = ea.Body.ToArray();
            string message = Encoding.UTF8.GetString(body);
            if (!ea.BasicProperties.Headers.TryGetValue("X-MsgType", out var headerValue))
            {
                Console.WriteLine("Message type header not found.");
                return;
            }

            var messageType = headerValue is byte[] headerBytes 
                ? Encoding.UTF8.GetString(headerBytes) 
                : string.Empty;
            try
            {
                // Process the message based on its type
                if (messageType == "OrderEvent")
                {
                    var orderEvent = JsonSerializer.Deserialize<OrderEvent>(message);
                    if (orderEvent != null)
                    {
                        var eventProcessor = new EventProcessing.EventProcessor();
                        await eventProcessor.ProcessOrderEventAsync(orderEvent.Id, orderEvent.Product, orderEvent.Total, orderEvent.Currency);
                    }
                }
                else if (messageType == "PaymentEvent")
                {
                    var paymentEvent = JsonSerializer.Deserialize<PaymentEvent>(message);
                    if (paymentEvent != null)
                    {
                        var eventProcessor = new EventProcessing.EventProcessor();
                        await eventProcessor.ProcessPaymentEventAsync(paymentEvent.Id, paymentEvent.Amount);
                    }
                }
                await channel.BasicAckAsync(ea.DeliveryTag, false);
            }

            catch (Exception ex)
            {
                Console.WriteLine($"Error processing message: {ex.Message}");
            }
        };

        await channel.BasicConsumeAsync("OrderQueue", false, consumer);
        await channel.BasicConsumeAsync("PaymentQueue", false, consumer);

        Console.WriteLine("Listening for messages.");
        await Task.Delay(Timeout.Infinite);

    }
}