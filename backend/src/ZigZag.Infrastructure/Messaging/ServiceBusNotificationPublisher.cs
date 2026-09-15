using System.Text.Json;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Options;
using ZigZag.Application.Common.Interfaces;

namespace ZigZag.Infrastructure.Messaging;

public sealed class ServiceBusNotificationPublisher : INotificationMessagePublisher, IAsyncDisposable
{
    private readonly ServiceBusClient _client;
    private readonly ServiceBusSender _sender;

    public ServiceBusNotificationPublisher(IOptions<ServiceBusSettings> settings)
    {
        var value = settings.Value;
        _client = new ServiceBusClient(value.ConnectionString);
        _sender = _client.CreateSender(value.QueueName);
    }

    public async Task PublishAsync(NotificationMessage message, CancellationToken cancellationToken = default)
    {
        var body = JsonSerializer.SerializeToUtf8Bytes(message);
        await _sender.SendMessageAsync(new ServiceBusMessage(body), cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        await _sender.DisposeAsync();
        await _client.DisposeAsync();
    }
}
