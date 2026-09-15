using System.Text.Json;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ZigZag.Application.Common.Interfaces;
using ZigZag.Domain.Entities;

namespace ZigZag.Infrastructure.Messaging;

/// <summary>
/// Consumes <see cref="NotificationMessage"/>s published by
/// <see cref="ServiceBusNotificationPublisher"/> and performs the actual
/// <see cref="INotificationRepository.CreateAsync"/> write - the other half
/// of moving notification creation off the request path and onto a queue.
/// Only registered by DependencyInjection.AddInfrastructure when
/// ServiceBus:ConnectionString is actually configured, so an environment
/// that hasn't set it up yet still starts normally.
/// </summary>
public sealed partial class NotificationConsumerBackgroundService : BackgroundService
{
    private readonly ServiceBusClient _client;
    private readonly ServiceBusSettings _settings;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<NotificationConsumerBackgroundService> _logger;
    private ServiceBusProcessor? _processor;

    public NotificationConsumerBackgroundService(
        IOptions<ServiceBusSettings> settings,
        IServiceScopeFactory scopeFactory,
        ILogger<NotificationConsumerBackgroundService> logger)
    {
        _settings = settings.Value;
        _client = new ServiceBusClient(_settings.ConnectionString);
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _processor = _client.CreateProcessor(_settings.QueueName);
        _processor.ProcessMessageAsync += HandleMessageAsync;
        _processor.ProcessErrorAsync += HandleErrorAsync;

        await _processor.StartProcessingAsync(stoppingToken);

        // Idles here for the lifetime of the host; StopAsync below tears the
        // processor down on shutdown.
        try
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, stoppingToken);
        }
        catch (OperationCanceledException)
        {
        }
    }

    private async Task HandleMessageAsync(ProcessMessageEventArgs args)
    {
        var payload = JsonSerializer.Deserialize<NotificationMessage>(args.Message.Body)
            ?? throw new InvalidOperationException("Received an empty notification message.");

        using var scope = _scopeFactory.CreateScope();
        var notificationRepository = scope.ServiceProvider.GetRequiredService<INotificationRepository>();

        await notificationRepository.CreateAsync(new Notification
        {
            Id = Guid.NewGuid(),
            UserId = payload.UserId,
            Type = payload.Type,
            Title = payload.Title,
            Message = payload.Message,
            TaskId = payload.TaskId,
            ProjectId = payload.ProjectId,
            IsRead = false,
            CreatedAt = DateTimeOffset.UtcNow,
        }, args.CancellationToken);

        await args.CompleteMessageAsync(args.Message, args.CancellationToken);
    }

    private Task HandleErrorAsync(ProcessErrorEventArgs args)
    {
        LogProcessingError(args.Exception, args.EntityPath);
        return Task.CompletedTask;
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "Service Bus notification processing error on {EntityPath}")]
    private partial void LogProcessingError(Exception exception, string entityPath);

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_processor is not null)
        {
            await _processor.StopProcessingAsync(cancellationToken);
            await _processor.DisposeAsync();
        }
        await _client.DisposeAsync();
        await base.StopAsync(cancellationToken);
    }
}
