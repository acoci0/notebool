namespace NotMarket.Api.Services;

public interface INoteDocumentStorage
{
    Task<string> SaveOriginalAsync(
        Guid sellerId,
        IFormFile file,
        CancellationToken cancellationToken);

    Task<string> SaveGeneratedAsync(
        Guid noteSubmissionId,
        Stream content,
        CancellationToken cancellationToken);

    Task<Stream?> OpenReadAsync(
        string relativePath,
        CancellationToken cancellationToken);

    Task DeleteAsync(
        string relativePath,
        CancellationToken cancellationToken);
}
