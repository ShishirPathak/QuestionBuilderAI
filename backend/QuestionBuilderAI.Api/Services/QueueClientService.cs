using System.Text.Json;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;

namespace QuestionBuilderAI.Api.Services
{
    public class QueueClientService
    {
        private readonly ServiceBusClient _client;
        private readonly ServiceBusSender _sender;
        public QueueClientService(string connectionString, string queueName)
        {
            _client = new ServiceBusClient(connectionString);
            _sender = _client.CreateSender(queueName);
        }

        public async Task EnqueueAsync(object payload)
        {
            var text = JsonSerializer.Serialize(payload);
            var message = new ServiceBusMessage(text);
            await _sender.SendMessageAsync(message);
        }
    }
}