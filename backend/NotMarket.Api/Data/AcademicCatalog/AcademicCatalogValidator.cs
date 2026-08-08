using System.Text.RegularExpressions;

namespace NotMarket.Api.Data.AcademicCatalog;

public sealed class AcademicCatalogValidationResult
{
    public List<string> Errors { get; } =
        [];

    public List<string> Warnings { get; } =
        [];

    public bool IsValid =>
        Errors.Count == 0;
}

public sealed class AcademicCatalogValidator
{
    private static readonly HashSet<string>
        ExpectedUniversityKeys =
            new(
                new[]
                {
                    "MARMARA",
                    "YTU",
                    "ITU",
                    "HACETTEPE",
                    "ESTU",
                    "BOUN",
                    "METU",
                    "ANADOLU",
                    "SELCUK",
                    "ISTANBUL"
                },
                StringComparer.OrdinalIgnoreCase);

    private static readonly Regex CatalogKeyRegex =
        new(
            "^[A-Z0-9]+(?:-[A-Z0-9]+)*$",
            RegexOptions.Compiled |
            RegexOptions.CultureInvariant);

    public AcademicCatalogValidationResult Validate(
        AcademicCatalogPackage package)
    {
        var result =
            new AcademicCatalogValidationResult();

        ValidateManifest(
            package.Manifest,
            result);

        ValidateUniversities(
            package,
            result);

        return result;
    }

    private static void ValidateManifest(
        AcademicCatalogManifest manifest,
        AcademicCatalogValidationResult result)
    {
        if (string.IsNullOrWhiteSpace(manifest.Version))
        {
            result.Errors.Add(
                "Manifest version boş olamaz.");
        }

        if (
            !string.Equals(
                manifest.CountryCode,
                "TR",
                StringComparison.OrdinalIgnoreCase)
        )
        {
            result.Errors.Add(
                "Manifest countryCode TR olmalıdır.");
        }

        if (manifest.ReferenceYear <= 0)
        {
            result.Errors.Add(
                "Manifest referenceYear geçersiz.");
        }

        if (
            !string.Equals(
                manifest.ScopeMode,
                "SelectedUniversities",
                StringComparison.OrdinalIgnoreCase)
        )
        {
            result.Errors.Add(
                "scopeMode SelectedUniversities olmalıdır.");
        }

        if (!manifest.IncludeFaculties)
        {
            result.Errors.Add(
                "includeFaculties true olmalıdır.");
        }

        if (!manifest.IncludeUndergraduatePrograms)
        {
            result.Errors.Add(
                "includeUndergraduatePrograms true olmalıdır.");
        }

        if (manifest.IncludeVocationalSchools)
        {
            result.Errors.Add(
                "İlk katalog sürümünde vocational school " +
                "kapsam dışıdır.");
        }

        if (manifest.IncludeGraduateInstitutes)
        {
            result.Errors.Add(
                "Graduate institute kapsam dışıdır.");
        }

        if (manifest.IncludeGraduatePrograms)
        {
            result.Errors.Add(
                "Graduate program kapsam dışıdır.");
        }

        if (manifest.IncludeResearchCenters)
        {
            result.Errors.Add(
                "Research center kapsam dışıdır.");
        }

        if (manifest.IncludeDoubleMajorPrograms)
        {
            result.Errors.Add(
                "Double major/minor programlar kapsam dışıdır.");
        }

        if (manifest.UniversityFiles.Count != 10)
        {
            result.Errors.Add(
                $"Manifest tam olarak 10 üniversite " +
                $"dosyası içermelidir. Mevcut: " +
                $"{manifest.UniversityFiles.Count}");
        }

        var distinctFiles =
            manifest.UniversityFiles
                .Distinct(
                    StringComparer.OrdinalIgnoreCase)
                .Count();

        if (
            distinctFiles !=
            manifest.UniversityFiles.Count
        )
        {
            result.Errors.Add(
                "Manifestte mükerrer üniversite " +
                "dosya adı bulunuyor.");
        }
    }

    private static void ValidateUniversities(
        AcademicCatalogPackage package,
        AcademicCatalogValidationResult result)
    {
        if (
            package.Universities.Count !=
            package.Manifest.UniversityFiles.Count
        )
        {
            result.Errors.Add(
                "Yüklenen üniversite sayısı manifest ile " +
                "uyuşmuyor.");
        }

        var universityKeys =
            new HashSet<string>(
                StringComparer.OrdinalIgnoreCase);

        var unitKeys =
            new HashSet<string>(
                StringComparer.OrdinalIgnoreCase);

        var programKeys =
            new HashSet<string>(
                StringComparer.OrdinalIgnoreCase);

        foreach (
            var university
            in package.Universities)
        {
            ValidateCatalogKey(
                university.CatalogKey,
                "University",
                universityKeys,
                result);

            if (
                string.IsNullOrWhiteSpace(
                    university.OfficialName)
            )
            {
                result.Errors.Add(
                    $"Üniversite adı boş: " +
                    $"{university.CatalogKey}");
            }

            if (
                university.OfficialName.Contains(
                    "İstanbul Üniversitesi-Cerrahpaşa",
                    StringComparison.OrdinalIgnoreCase)
            )
            {
                result.Errors.Add(
                    "İstanbul Üniversitesi-Cerrahpaşa " +
                    "bu katalog kapsamına dahil edilmemelidir.");
            }

            if (
                !string.Equals(
                    university.CountryCode,
                    "TR",
                    StringComparison.OrdinalIgnoreCase)
            )
            {
                result.Errors.Add(
                    $"{university.CatalogKey}: " +
                    "countryCode TR olmalıdır.");
            }

            if (university.VerifiedAt is null)
            {
                result.Warnings.Add(
                    $"{university.CatalogKey}: " +
                    "VerifiedAt belirtilmemiş.");
            }

            ValidateSourceUrls(
                university.SourceUrls,
                university.CatalogKey,
                result);

            if (
                university.IsActive &&
                university.Units.Count == 0
            )
            {
                result.Errors.Add(
                    $"{university.CatalogKey}: " +
                    "aktif üniversitenin fakültesi yok.");
            }

            ValidateAliases(
                university.Aliases,
                university.CatalogKey,
                result);

            foreach (
                var unit
                in university.Units)
            {
                ValidateUnit(
                    university,
                    unit,
                    unitKeys,
                    programKeys,
                    result);
            }
        }

        var missingExpectedKeys =
            ExpectedUniversityKeys
                .Except(
                    universityKeys,
                    StringComparer.OrdinalIgnoreCase)
                .ToArray();

        if (missingExpectedKeys.Length > 0)
        {
            result.Errors.Add(
                "Beklenen üniversiteler eksik: " +
                string.Join(
                    ", ",
                    missingExpectedKeys));
        }

        var unexpectedKeys =
            universityKeys
                .Except(
                    ExpectedUniversityKeys,
                    StringComparer.OrdinalIgnoreCase)
                .ToArray();

        if (unexpectedKeys.Length > 0)
        {
            result.Errors.Add(
                "Kapsam dışında üniversite CatalogKey: " +
                string.Join(
                    ", ",
                    unexpectedKeys));
        }
    }

    private static void ValidateUnit(
        AcademicCatalogUniversity university,
        AcademicCatalogUnit unit,
        HashSet<string> unitKeys,
        HashSet<string> programKeys,
        AcademicCatalogValidationResult result)
    {
        ValidateCatalogKey(
            unit.CatalogKey,
            "Unit",
            unitKeys,
            result);

        if (string.IsNullOrWhiteSpace(unit.Name))
        {
            result.Errors.Add(
                $"{unit.CatalogKey}: unit adı boş.");
        }

        /*
         * İlk katalog sürümünde yalnızca faculty
         * student-facing academic unit olarak
         * tutulacak.
         */
        if (
            !string.Equals(
                unit.UnitType,
                "Faculty",
                StringComparison.OrdinalIgnoreCase)
        )
        {
            result.Errors.Add(
                $"{unit.CatalogKey}: " +
                $"desteklenmeyen UnitType " +
                $"'{unit.UnitType}'.");
        }

        if (
            unit.IsActive &&
            unit.Programs.Count == 0
        )
        {
            result.Errors.Add(
                $"{unit.CatalogKey}: " +
                "aktif fakültenin lisans programı yok.");
        }

        ValidateSourceUrls(
            unit.SourceUrls,
            unit.CatalogKey,
            result,
            allowEmpty: true);

        foreach (
            var program
            in unit.Programs)
        {
            ValidateProgram(
                university,
                unit,
                program,
                programKeys,
                result);
        }
    }

    private static void ValidateProgram(
        AcademicCatalogUniversity university,
        AcademicCatalogUnit unit,
        AcademicCatalogProgram program,
        HashSet<string> programKeys,
        AcademicCatalogValidationResult result)
    {
        ValidateCatalogKey(
            program.CatalogKey,
            "Program",
            programKeys,
            result);

        if (
            string.IsNullOrWhiteSpace(
                program.Name)
        )
        {
            result.Errors.Add(
                $"{program.CatalogKey}: " +
                "program adı boş.");
        }

        if (
            !string.Equals(
                program.DegreeLevel,
                "Bachelor",
                StringComparison.OrdinalIgnoreCase)
        )
        {
            result.Errors.Add(
                $"{program.CatalogKey}: " +
                $"DegreeLevel Bachelor olmalıdır.");
        }

        if (
            !program.IsActive &&
            program.IsSelectable
        )
        {
            result.Errors.Add(
                $"{program.CatalogKey}: " +
                "pasif program seçilebilir olamaz.");
        }

        ValidateAliases(
            program.Aliases,
            program.CatalogKey,
            result);

        ValidateSourceUrls(
            program.SourceUrls,
            program.CatalogKey,
            result,
            allowEmpty: true);

        /*
         * Hiyerarşik CatalogKey kullanımı zorunlu
         * değil fakat yanlış eşleşmeleri erken
         * yakalamak için uyarı üretir.
         */
        if (
            !program.CatalogKey.StartsWith(
                unit.CatalogKey + "-",
                StringComparison.OrdinalIgnoreCase)
        )
        {
            result.Warnings.Add(
                $"{program.CatalogKey}: " +
                $"program CatalogKey'i parent unit " +
                $"anahtarıyla başlamıyor.");
        }

        if (
            !unit.CatalogKey.StartsWith(
                university.CatalogKey + "-",
                StringComparison.OrdinalIgnoreCase)
        )
        {
            result.Warnings.Add(
                $"{unit.CatalogKey}: " +
                $"unit CatalogKey'i parent university " +
                $"anahtarıyla başlamıyor.");
        }
    }

    private static void ValidateCatalogKey(
        string catalogKey,
        string entityType,
        HashSet<string> usedKeys,
        AcademicCatalogValidationResult result)
    {
        if (string.IsNullOrWhiteSpace(catalogKey))
        {
            result.Errors.Add(
                $"{entityType} CatalogKey boş olamaz.");

            return;
        }

        var normalizedKey =
            catalogKey.Trim().ToUpperInvariant();

        if (!CatalogKeyRegex.IsMatch(normalizedKey))
        {
            result.Errors.Add(
                $"{entityType} CatalogKey geçersiz: " +
                $"{catalogKey}");
        }

        if (!usedKeys.Add(normalizedKey))
        {
            result.Errors.Add(
                $"Mükerrer {entityType} CatalogKey: " +
                $"{catalogKey}");
        }
    }

    private static void ValidateAliases(
        IEnumerable<string> aliases,
        string ownerKey,
        AcademicCatalogValidationResult result)
    {
        var seen =
            new HashSet<string>(
                StringComparer.OrdinalIgnoreCase);

        foreach (var alias in aliases)
        {
            if (string.IsNullOrWhiteSpace(alias))
            {
                result.Errors.Add(
                    $"{ownerKey}: boş alias bulunuyor.");

                continue;
            }

            var trimmed =
                alias.Trim();

            if (!seen.Add(trimmed))
            {
                result.Warnings.Add(
                    $"{ownerKey}: mükerrer alias: " +
                    $"{trimmed}");
            }
        }
    }

    private static void ValidateSourceUrls(
        IEnumerable<string> urls,
        string ownerKey,
        AcademicCatalogValidationResult result,
        bool allowEmpty = false)
    {
        var values =
            urls
                .Where(
                    x =>
                        !string.IsNullOrWhiteSpace(x))
                .ToArray();

        if (
            values.Length == 0 &&
            !allowEmpty
        )
        {
            result.Warnings.Add(
                $"{ownerKey}: resmî kaynak URL'si yok.");

            return;
        }

        foreach (var url in values)
        {
            if (
                !Uri.TryCreate(
                    url,
                    UriKind.Absolute,
                    out var uri) ||
                (
                    uri.Scheme != Uri.UriSchemeHttps &&
                    uri.Scheme != Uri.UriSchemeHttp
                )
            )
            {
                result.Errors.Add(
                    $"{ownerKey}: geçersiz kaynak URL: " +
                    $"{url}");
            }
        }
    }
}