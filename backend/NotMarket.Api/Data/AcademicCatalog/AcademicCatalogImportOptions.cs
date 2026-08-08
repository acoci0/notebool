namespace NotMarket.Api.Data.AcademicCatalog;

public sealed class AcademicCatalogImportOptions
{
    public const string SectionName =
        "AcademicCatalog";

    public string CatalogDirectory { get; set; } =
        "Data/AcademicCatalog/Catalogs/2026.1";

    public bool DryRun { get; set; } =
        true;

    public bool DeactivateMissingUniversities
    {
        get;
        set;
    } = true;

    public bool ImportOnStartup { get; set; } =
        false;
}