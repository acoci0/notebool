namespace NotMarket.Api.Services;

public sealed class NotePdfGenerationOptions
{
    public const string SectionName =
        "NotePdfGeneration";

    /*
     * PDF üretim sistemi gerektiğinde
     * yapılandırmadan kapatılabilir.
     */
    public bool Enabled { get; set; } =
        true;

    /*
     * İçeriği yapılandırılmış belge modeline
     * dönüştürecek OpenAI modeli.
     */
    public string Model { get; set; } =
        "gpt-5.4";

    public string PromptVersion { get; set; } =
        "note-pdf-conversion-v1";

    /*
     * Yapılandırılmış içerik üretiminin
     * maksimum çıktı token sınırı.
     */
    public int MaxOutputTokens { get; set; } =
        12000;

    /*
     * LaTeX derleyicisinin çalıştırılabilir
     * dosya adı veya tam yolu.
     */
    public string CompilerPath { get; set; } =
        "tectonic";

    /*
     * Tek bir PDF derleme işleminin
     * maksimum çalışma süresi.
     */
    public int TimeoutSeconds { get; set; } =
        120;

    /*
     * Üretilebilecek LaTeX kaynak metninin
     * maksimum karakter uzunluğu.
     */
    public int MaxSourceCharacters { get; set; } =
        200000;

    /*
     * Oluşturulan PDF için maksimum
     * dosya boyutu: 50 MB.
     */
    public int MaxGeneratedPdfBytes { get; set; } =
        50 * 1024 * 1024;

    /*
     * Başarısız derleme dosyalarının
     * geliştirme amacıyla saklanıp
     * saklanmayacağını belirler.
     */
    public bool KeepFailedArtifacts { get; set; } =
        false;

    /*
     * Backend içerisinde kullanılan sabit
     * LaTeX şablonunun sürümü.
     */
    public string TemplateVersion { get; set; } =
        "note-pdf-template-v1";
}
