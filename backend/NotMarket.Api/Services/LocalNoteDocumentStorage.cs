namespace NotMarket.Api.Services;

public sealed class LocalNoteDocumentStorage(
    IWebHostEnvironment environment)
    : INoteDocumentStorage
{
    private readonly string _rootPath =
        Path.Combine(
            environment.ContentRootPath,
            "App_Data",
            "notes");

    public async Task<string> SaveOriginalAsync(
        Guid sellerId,
        IFormFile file,
        CancellationToken cancellationToken)
    {
        var directory =
            Path.Combine(
                _rootPath,
                "original",
                sellerId.ToString(),
                DateTime.UtcNow.Year.ToString(),
                DateTime.UtcNow.Month.ToString("00"));

        Directory.CreateDirectory(
            directory);

        var fileName =
            $"{Guid.NewGuid():N}.pdf";

        var fullPath =
            Path.Combine(
                directory,
                fileName);

        await using var target =
            CreateWriteStream(
                fullPath);

        await file.CopyToAsync(
            target,
            cancellationToken);

        return ToRelativePath(
            fullPath);
    }

    public async Task<string> SaveGeneratedAsync(
        Guid noteSubmissionId,
        Stream content,
        CancellationToken cancellationToken)
    {
        var directory =
            Path.Combine(
                _rootPath,
                "generated",
                noteSubmissionId.ToString());

        Directory.CreateDirectory(
            directory);

        var fileName =
            $"{Guid.NewGuid():N}.pdf";

        var fullPath =
            Path.Combine(
                directory,
                fileName);

        await using var target =
            CreateWriteStream(
                fullPath);

        await content.CopyToAsync(
            target,
            cancellationToken);

        return ToRelativePath(
            fullPath);
    }

    public Task<Stream?> OpenReadAsync(
        string relativePath,
        CancellationToken cancellationToken)
    {
        cancellationToken
            .ThrowIfCancellationRequested();

        if (!TryResolvePath(
                relativePath,
                out var fullPath))
        {
            return Task.FromResult<Stream?>(
                null);
        }

        if (!File.Exists(fullPath))
        {
            return Task.FromResult<Stream?>(
                null);
        }

        Stream stream =
            new FileStream(
                fullPath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                81920,
                useAsync: true);

        return Task.FromResult<Stream?>(
            stream);
    }

    public Task DeleteAsync(
        string relativePath,
        CancellationToken cancellationToken)
    {
        cancellationToken
            .ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(
                relativePath))
        {
            return Task.CompletedTask;
        }

        if (!TryResolvePath(
                relativePath,
                out var fullPath))
        {
            throw new InvalidOperationException(
                "Geçersiz not dosyası yolu.");
        }

        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }

        return Task.CompletedTask;
    }

    private static FileStream CreateWriteStream(
        string fullPath)
    {
        return new FileStream(
            fullPath,
            FileMode.CreateNew,
            FileAccess.Write,
            FileShare.None,
            81920,
            useAsync: true);
    }

    private string ToRelativePath(
        string fullPath)
    {
        return Path.GetRelativePath(
                _rootPath,
                fullPath)
            .Replace(
                '\\',
                '/');
    }

    private bool TryResolvePath(
        string relativePath,
        out string fullPath)
    {
        fullPath =
            string.Empty;

        if (string.IsNullOrWhiteSpace(
                relativePath))
        {
            return false;
        }

        var root =
            Path.GetFullPath(
                _rootPath);

        var candidate =
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

        if (!candidate.StartsWith(
                rootWithSeparator,
                StringComparison.Ordinal))
        {
            return false;
        }

        fullPath =
            candidate;

        return true;
    }
}
