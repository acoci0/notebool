using System.ComponentModel.DataAnnotations;

namespace NotMarket.Api.Domain;

public sealed class AcademicUniversity
{
    public Guid Id { get; set; } =
        Guid.NewGuid();

    /*
     * Kullanıcıya gösterilecek canonical/resmî isim.
     *
     * Örnek:
     * Marmara Üniversitesi
     */
    [MaxLength(250)]
    public required string Name { get; set; }

    /*
     * Arama ve mükerrer kayıt kontrolü için
     * normalize edilmiş üniversite adı.
     *
     * Örnek:
     * marmara universitesi
     */
    [MaxLength(250)]
    public required string NormalizedName { get; set; }

    /*
     * Üniversite ülkesi.
     *
     * Bu modülde yalnızca Türkiye üniversiteleri
     * kullanılacağı için varsayılan değer TR'dir.
     */
    [MaxLength(2)]
    public string CountryCode { get; set; } =
        "TR";

    /*
     * Pasif üniversiteler arama sonuçlarına
     * dahil edilmez.
     */
    public bool IsActive { get; set; } =
        true;

    public DateTimeOffset CreatedAt { get; set; } =
        DateTimeOffset.UtcNow;

    public DateTimeOffset? UpdatedAt { get; set; }

    /*
 * Üniversiteye bağlı fakülte, enstitü,
 * yüksekokul ve diğer akademik birimler.
 */
    public ICollection<AcademicUnit>
        AcademicUnits { get; set; } =
            new List<AcademicUnit>();
    
    /*
     * Bu üniversiteye bağlı öğrenci
     * doğrulamalarının navigation alanı.
     */
    public ICollection<StudentVerification>
        StudentVerifications { get; set; } =
            new List<StudentVerification>();
}