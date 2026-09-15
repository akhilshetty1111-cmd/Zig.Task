namespace ZigZag.Application.Common.Interfaces;

/// <summary>
/// Stores and removes task attachment files in blob storage. Implemented in
/// Infrastructure (Azure Blob Storage) - Application never touches the Azure
/// SDK directly, same reasoning as <see cref="ITokenService"/>.
/// </summary>
public interface IBlobStorageService
{
    /// <returns>The publicly reachable URL of the uploaded blob.</returns>
    Task<string> UploadAsync(
        Stream content, string blobName, string contentType, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes the blob referenced by a URL this service previously returned
    /// from <see cref="UploadAsync"/> - no separate blob-name column is kept
    /// alongside it, since the blob name can always be recovered from the URL.
    /// </summary>
    Task DeleteAsync(string blobUrl, CancellationToken cancellationToken = default);
}
