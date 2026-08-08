namespace NotMarket.Api.Contracts;

/*
 * Üniversite arama sonucunda frontend'e
 * gönderilen canonical üniversite kaydı.
 */
public sealed record AcademicUniversityDto(
    Guid Id,
    string Name);

/*
 * Seçilen üniversiteye bağlı fakülte,
 * enstitü, yüksekokul ve diğer akademik
 * birimleri temsil eder.
 */
public sealed record AcademicUnitDto(
    Guid Id,
    Guid UniversityId,
    string Name,
    string UnitType);

/*
 * Seçilen akademik birime bağlı bölüm
 * veya programı temsil eder.
 */
public sealed record AcademicProgramDto(
    Guid Id,
    Guid AcademicUnitId,
    string Name);