using System.ComponentModel.DataAnnotations;

namespace NotMarket.Api.Domain;

public sealed class NoteSubmission
{
    public Guid Id { get; set; } =
        Guid.NewGuid();

    public Guid RequestId { get; set; }

    public NoteRequest Request { get; set; } =
        null!;

    public Guid SellerId { get; set; }

    public ApplicationUser Seller { get; set; } =
        null!;

    [MaxLength(220)]
    public required string Title { get; set; }

    [MaxLength(500)]
    public required string OriginalBlobPath { get; set; }

    [MaxLength(500)]
    public string? GeneratedPdfBlobPath { get; set; }

    public int MatchScore { get; set; }

    public int ReadabilityScore { get; set; }

    public int OriginalityRiskScore { get; set; }

    public decimal? SalePrice { get; set; }

    public NoteSubmissionStatus Status { get; set; } =
        NoteSubmissionStatus.Uploaded;

    [MaxLength(800)]
    public string? ReviewNote { get; set; }

    public Guid? ReviewedByUserId { get; set; }

    public DateTimeOffset? ReviewedAt { get; set; }

    /*
     * PDF üretim işleminin kaç kez
     * başlatıldığını gösterir.
     */
    public int PdfGenerationAttemptCount { get; set; }

    /*
     * Son PDF üretim hatasının teknik özeti.
     * Bu alan doğrudan son kullanıcıya
     * gösterilmemelidir.
     */
    [MaxLength(2000)]
    public string? PdfGenerationError { get; set; }

    /*
     * GeneratedPdfBlobPath içerisindeki
     * PDF'in oluşturulma zamanı.
     */
    public DateTimeOffset? PdfGeneratedAt { get; set; }

    /*
     * İçerik dönüştürmede kullanılan
     * OpenAI modeli.
     */
    [MaxLength(100)]
    public string? PdfGenerationModelName { get; set; }

    /*
    * İçerik dönüştürmede kullanılan
    * prompt sürümü.
    */
    [MaxLength(100)]
    public string? PdfConversionPromptVersion { get; set; }

    /*
     * Kullanılan sabit LaTeX şablon sürümü.
     */
    [MaxLength(100)]
    public string? PdfTemplateVersion { get; set; }

    /*
     * PDF üretiminde kullanılan derleyici.
     */
    [MaxLength(100)]
    public string? PdfCompilerName { get; set; }

    public DateTimeOffset CreatedAt { get; set; } =
        DateTimeOffset.UtcNow;

    public ICollection<NoteAiReview> AiReviews { get; set; } =
        [];

    public ICollection<Order> Orders { get; set; } =
        [];
}