namespace NotMarket.Api.Services;

public sealed class LocalVerificationDocumentStorage(
    IWebHostEnvironment environment)
    : IVerificationDocumentStorage
{
    private readonly string _rootPath = Path.Combine(
        environment.ContentRootPath,
        "App_Data",
        "student-verifications");

    public async Task<string> SaveAsync(
        Guid userId,
        IFormFile file,
        CancellationToken cancellationToken)
    {
        var directory = Path.Combine(
            _rootPath,
            userId.ToString(),
            DateTime.UtcNow.Year.ToString(),
            DateTime.UtcNow.Month.ToString("00"));

        Directory.CreateDirectory(directory);

        var fileName = $"{Guid.NewGuid():N}.pdf";

        var fullPath = Path.Combine(
            directory,
            fileName);

        await using var target = new FileStream(
            fullPath,
            FileMode.CreateNew,
            FileAccess.Write,
            FileShare.None,
            81920,
            useAsync: true);

        await file.CopyToAsync(
            target,
            cancellationToken);

        return Path.GetRelativePath(
                _rootPath,
                fullPath)
            .Replace('\\', '/');
    }

    public Task<Stream?> OpenReadAsync(
        string relativePath,
        CancellationToken cancellationToken)
    {
        var fullPath = Path.GetFullPath(
            Path.Combine(_rootPath, relativePath));

        var root = Path.GetFullPath(_rootPath);

        if (!fullPath.StartsWith(
                root,
                StringComparison.Ordinal))
        {
            return Task.FromResult<Stream?>(null);
        }

        if (!File.Exists(fullPath))
        {
            return Task.FromResult<Stream?>(null);
        }

        Stream stream = new FileStream(
            fullPath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            81920,
            useAsync: true);

        return Task.FromResult<Stream?>(stream);
    }

    public Task DeleteAsync(
        string relativePath,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(relativePath))
        {
            return Task.CompletedTask;
        }

        var root =
            Path.GetFullPath(_rootPath);

        var fullPath =
            Path.GetFullPath(
                Path.Combine(
                    root,
                    relativePath));

        var rootWithSeparator =
            root.EndsWith(
                Path.DirectorySeparatorChar)
                ? root
                : root +
                Path.DirectorySeparatorChar;

        if (!fullPath.StartsWith(
                rootWithSeparator,
                StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "Geçersiz doğrulama belgesi yolu.");
        }

        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }

        return Task.CompletedTask;
    }
}