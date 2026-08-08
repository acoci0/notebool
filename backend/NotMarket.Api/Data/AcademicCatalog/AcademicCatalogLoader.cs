using System.Text.Json;
using Microsoft.Extensions.Options;

namespace NotMarket.Api.Data.AcademicCatalog;

public sealed class AcademicCatalogLoader
{
    private readonly AcademicCatalogImportOptions _options;

    private readonly IWebHostEnvironment _environment;

    private static readonly JsonSerializerOptions
        JsonOptions =
            new(JsonSerializerDefaults.Web)
            {
                PropertyNameCaseInsensitive = true,
                ReadCommentHandling =
                    JsonCommentHandling.Skip,
                AllowTrailingCommas = false
            };

    public AcademicCatalogLoader(
        IOptions<AcademicCatalogImportOptions> options,
        IWebHostEnvironment environment)
    {
        _options = options.Value;
        _environment = environment;
    }

    public async Task<AcademicCatalogPackage> LoadAsync(
        CancellationToken cancellationToken = default)
    {
        var catalogDirectory =
            ResolveCatalogDirectory();

        var manifestPath =
            Path.Combine(
                catalogDirectory,
                "manifest.json");

        if (!File.Exists(manifestPath))
        {
            throw new FileNotFoundException(
                "Academic catalog manifest bulunamadı.",
                manifestPath);
        }

        var manifest =
            await ReadJsonAsync<AcademicCatalogManifest>(
                manifestPath,
                cancellationToken);

        var universities =
            new List<AcademicCatalogUniversity>();

        foreach (
            var universityFile
            in manifest.UniversityFiles)
        {
            ValidateManifestFileName(
                universityFile);

            var universityPath =
                Path.Combine(
                    catalogDirectory,
                    universityFile);

            if (!File.Exists(universityPath))
            {
                throw new FileNotFoundException(
                    $"Manifestte bulunan katalog dosyası " +
                    $"bulunamadı: {universityFile}",
                    universityPath);
            }

            var university =
                await ReadJsonAsync<
                    AcademicCatalogUniversity>(
                        universityPath,
                        cancellationToken);

            universities.Add(
                university);
        }

        return new AcademicCatalogPackage
        {
            Manifest =
                manifest,

            CatalogDirectory =
                catalogDirectory,

            Universities =
                universities
        };
    }

    private string ResolveCatalogDirectory()
    {
        var configuredPath =
            _options.CatalogDirectory.Trim();

        if (string.IsNullOrWhiteSpace(configuredPath))
        {
            throw new InvalidOperationException(
                "AcademicCatalog:CatalogDirectory boş olamaz.");
        }

        if (Path.IsPathRooted(configuredPath))
        {
            return EnsureDirectoryExists(
                Path.GetFullPath(configuredPath));
        }

        /*
         * Development sırasında ContentRootPath
         * backend/NotMarket.Api dizinidir.
         */
        var contentRootCandidate =
            Path.GetFullPath(
                Path.Combine(
                    _environment.ContentRootPath,
                    configuredPath));

        if (Directory.Exists(contentRootCandidate))
        {
            return contentRootCandidate;
        }

        /*
         * Publish/build çıktısından çalıştırıldığında
         * katalog AppContext.BaseDirectory altında
         * bulunabilir.
         */
        var outputCandidate =
            Path.GetFullPath(
                Path.Combine(
                    AppContext.BaseDirectory,
                    configuredPath));

        return EnsureDirectoryExists(
            outputCandidate);
    }

    private static string EnsureDirectoryExists(
        string path)
    {
        if (!Directory.Exists(path))
        {
            throw new DirectoryNotFoundException(
                $"Academic catalog dizini bulunamadı: {path}");
        }

        return path;
    }

    private static void ValidateManifestFileName(
        string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new InvalidDataException(
                "Manifest boş universityFiles kaydı içeriyor.");
        }

        /*
         * Manifest üzerinden katalog klasörü
         * dışına çıkılmasını engeller.
         */
        if (
            Path.IsPathRooted(fileName) ||
            fileName != Path.GetFileName(fileName))
        {
            throw new InvalidDataException(
                $"Geçersiz katalog dosya adı: {fileName}");
        }

        if (
            !fileName.EndsWith(
                ".json",
                StringComparison.OrdinalIgnoreCase)
        )
        {
            throw new InvalidDataException(
                $"Katalog dosyası JSON olmalıdır: {fileName}");
        }
    }

    private static async Task<T> ReadJsonAsync<T>(
        string path,
        CancellationToken cancellationToken)
    {
        await using var stream =
            File.OpenRead(path);

        var value =
            await JsonSerializer.DeserializeAsync<T>(
                stream,
                JsonOptions,
                cancellationToken);

        if (value is null)
        {
            throw new InvalidDataException(
                $"JSON deserialize edilemedi: {path}");
        }

        return value;
    }
}