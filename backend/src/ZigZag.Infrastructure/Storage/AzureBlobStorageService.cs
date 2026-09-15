using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Options;
using ZigZag.Application.Common.Interfaces;

namespace ZigZag.Infrastructure.Storage;

/// <summary>
/// Assumes the container already exists with "Blob" (anonymous read) public
/// access, created once via the Azure Portal - this service never creates it,
/// matching how every other piece of Azure infrastructure in this project was
/// provisioned manually rather than from code.
/// </summary>
public sealed class AzureBlobStorageService : IBlobStorageService
{
    private readonly BlobContainerClient _containerClient;

    public AzureBlobStorageService(IOptions<BlobStorageSettings> settings)
    {
        var value = settings.Value;
        _containerClient = new BlobContainerClient(value.ConnectionString, value.ContainerName);
    }

    public async Task<string> UploadAsync(
        Stream content, string blobName, string contentType, CancellationToken cancellationToken = default)
    {
        var blobClient = _containerClient.GetBlobClient(blobName);
        await blobClient.UploadAsync(
            content,
            new BlobHttpHeaders { ContentType = contentType },
            cancellationToken: cancellationToken);

        return blobClient.Uri.ToString();
    }

    public async Task DeleteAsync(string blobUrl, CancellationToken cancellationToken = default)
    {
        // The container name is a fixed, known prefix in every URL this service
        // itself produced, so the blob name is simply what follows it - no need
        // to persist a separate blob-name column alongside file_url.
        var containerPrefix = $"/{_containerClient.Name}/";
        var path = new Uri(blobUrl).AbsolutePath;
        var index = path.IndexOf(containerPrefix, StringComparison.Ordinal);
        if (index < 0)
        {
            return;
        }

        var blobName = Uri.UnescapeDataString(path[(index + containerPrefix.Length)..]);
        await _containerClient.DeleteBlobIfExistsAsync(blobName, cancellationToken: cancellationToken);
    }
}
