using System.Text.Json;
using Azure.Messaging.ServiceBus;

public class ServiceBusService
{
    private readonly ServiceBusClient _client;
    private const string QueueName = "todoevents";

    public ServiceBusService(ServiceBusClient client)
    {
        _client = client;
    }

    public async Task SendMessageAsync(TodoEvent todoEvent)
    {
        try
        {
            var sender = _client.CreateSender(QueueName);
            var messageBody = JsonSerializer.Serialize(todoEvent);
            var message = new ServiceBusMessage(messageBody)
            {
                ContentType = "application/json",
                Subject = todoEvent.EventType
            };

            await sender.SendMessageAsync(message);
            Console.WriteLine($"Sent message: {todoEvent.EventType} for Todo {todoEvent.Todo.Id}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error sending message to Service Bus: {ex.Message}");
            throw;
        }
    }
}