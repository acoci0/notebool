namespace NotMarket.Api.Data.AcademicCatalog;

public sealed class AcademicCatalogManifest
{
    public string Version { get; set; } =
        string.Empty;

    public string CountryCode { get; set; } =
        "TR";

    public int ReferenceYear { get; set; }

    public string ScopeMode { get; set; } =
        string.Empty;

    public string DepartmentDefinition { get; set; } =
        string.Empty;

    public bool IncludeFaculties { get; set; }

    public bool IncludeUndergraduatePrograms { get; set; }

    public bool IncludeOpenEducationFaculties { get; set; }

    public bool IncludeVocationalSchools { get; set; }

    public bool IncludeSchools { get; set; }

    public bool IncludeConservatories { get; set; }

    public bool IncludeGraduateInstitutes { get; set; }

    public bool IncludeGraduatePrograms { get; set; }

    public bool IncludeResearchCenters { get; set; }

    public bool IncludeDoubleMajorPrograms { get; set; }

    public bool DeactivateUniversitiesOutsideSelection
    {
        get;
        set;
    }

    public List<string> UniversityFiles { get; set; } =
        [];
}

public sealed class AcademicCatalogUniversity
{
    public string CatalogKey { get; set; } =
        string.Empty;

    public string OfficialName { get; set; } =
        string.Empty;

    public List<string> Aliases { get; set; } =
        [];

    public string CountryCode { get; set; } =
        "TR";

    public string? City { get; set; }

    public string? SourceName { get; set; } =
        "OfficialUniversityWebsite";

    public List<string> SourceUrls { get; set; } =
        [];

    public DateTimeOffset? VerifiedAt { get; set; }

    public bool IsActive { get; set; } =
        true;

    public List<AcademicCatalogUnit> Units
    {
        get;
        set;
    } = [];
}

public sealed class AcademicCatalogUnit
{
    public string CatalogKey { get; set; } =
        string.Empty;

    public string Name { get; set; } =
        string.Empty;

    public string UnitType { get; set; } =
        "Faculty";

    public string? SourceName { get; set; }

    public List<string> SourceUrls { get; set; } =
        [];

    public DateTimeOffset? VerifiedAt { get; set; }

    public bool IsActive { get; set; } =
        true;

    public List<AcademicCatalogProgram> Programs
    {
        get;
        set;
    } = [];
}

public sealed class AcademicCatalogProgram
{
    public string CatalogKey { get; set; } =
        string.Empty;

    public string Name { get; set; } =
        string.Empty;

    public string DegreeLevel { get; set; } =
        "Bachelor";

    public string? EducationLanguage { get; set; }

    public string? SourceName { get; set; }

    public List<string> SourceUrls { get; set; } =
        [];

    public DateTimeOffset? VerifiedAt { get; set; }

    public bool IsActive { get; set; } =
        true;

    public bool IsSelectable { get; set; } =
        true;

    public List<string> Aliases { get; set; } =
        [];
}

public sealed class AcademicCatalogPackage
{
    public required AcademicCatalogManifest Manifest
    {
        get;
        init;
    }

    public required string CatalogDirectory
    {
        get;
        init;
    }

    public List<AcademicCatalogUniversity> Universities
    {
        get;
        init;
    } = [];
}