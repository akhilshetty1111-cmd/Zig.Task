namespace ZigZag.Infrastructure.Storage;

/// <summary>Bound from the "BlobStorage" configuration section.</summary>
public sealed class BlobStorageSettings
{
    public const string SectionName = "BlobStorage";

    public required string ConnectionString { get; set; }
    public required string ContainerName { get; set; }
}
