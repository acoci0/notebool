namespace NotMarket.Api.Services;

public sealed class OpenAiOptions
{
    public const string SectionName =
        "OpenAI";

    /*
     * API anahtarı appsettings.json içerisinde
     * tutulmaz. User Secrets veya ortam
     * değişkenlerinden okunur.
     */
    public string ApiKey { get; set; } =
        string.Empty;

    public string BaseUrl { get; set; } =
        "https://api.openai.com/v1/";

    /*
     * Model yapılandırmadan değiştirilebilir.
     */
    public string Model { get; set; } =
        "gpt-5.4";

    public string PromptVersion { get; set; } =
        "note-review-v1";

    public int MaxOutputTokens { get; set; } =
        2500;

    /*
     * İlk sürümde en fazla 20 MB PDF.
     */
    public int MaxDocumentBytes { get; set; } =
        20 * 1024 * 1024;
}