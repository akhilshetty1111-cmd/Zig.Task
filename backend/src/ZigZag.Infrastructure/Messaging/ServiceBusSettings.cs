namespace ZigZag.Infrastructure.Messaging;

/// <summary>Bound from the "ServiceBus" configuration section.</summary>
public sealed class ServiceBusSettings
{
    public const string SectionName = "ServiceBus";

    public required string ConnectionString { get; set; }
    public required string QueueName { get; set; }
}
