namespace EventProcessing
{
    using System.Threading.Tasks;
    using RabbitMQ.Client;

    public class Program
    {
        public static async Task Main()
        {
            QueueProcessor queueProcessor = new QueueProcessor();
            await queueProcessor.ProcessQueueAsync();
        }
    }
}

