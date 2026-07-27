namespace NotMarket.Api.Services;

public interface IVerificationDocumentStorage
{
    Task<string> SaveAsync(
        Guid userId,
        IFormFile file,
        CancellationToken cancellationToken);

    Task<Stream?> OpenReadAsync(
        string relativePath,
        CancellationToken cancellationToken);
}