using System.ComponentModel.DataAnnotations;

namespace NotMarket.Api.Domain;

/*
 * Üniversiteye bağlı üst akademik birimi temsil eder.
 *
 * Örnekler:
 * - Fen Fakültesi
 * - Sosyal Bilimler Enstitüsü
 * - Bankacılık ve Sigortacılık Yüksekokulu
 * - Teknik Bilimler Meslek Yüksekokulu
 */
public sealed class AcademicUnit
{
    public Guid Id { get; set; } =
        Guid.NewGuid();

    /*
     * Birimin bağlı olduğu üniversite.
     */
    public Guid UniversityId { get; set; }

    public AcademicUniversity University { get; set; } =
        null!;

    /*
     * Kullanıcıya gösterilecek canonical isim.
     */
    [MaxLength(250)]
    public required string Name { get; set; }

    /*
     * Arama ve mükerrer kayıt kontrolünde
     * kullanılacak normalize edilmiş isim.
     */
    [MaxLength(250)]
    public required string NormalizedName { get; set; }

    /*
     * Birimin fakülte, enstitü, yüksekokul
     * gibi türünü belirtir.
     */
    public AcademicUnitType UnitType { get; set; } =
        AcademicUnitType.Faculty;

    public bool IsActive { get; set; } =
        true;

    public DateTimeOffset CreatedAt { get; set; } =
        DateTimeOffset.UtcNow;

    public DateTimeOffset? UpdatedAt { get; set; }

    /*
     * Bu akademik birime bağlı bölüm ve
     * programlar.
     */
    public ICollection<AcademicProgram>
        Programs
    { get; set; } =
            new List<AcademicProgram>();

    /*
     * Bu birimle oluşturulan öğrenci
     * doğrulama kayıtları.
     */
    public ICollection<StudentVerification>
        StudentVerifications
    { get; set; } =
            new List<StudentVerification>();
}