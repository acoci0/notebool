namespace NotMarket.Api.Services;

/*
 * OpenAI tarafından oluşturulan, ancak henüz
 * LaTeX kaynağına dönüştürülmemiş belge modeli.
 */
public sealed record NoteDocumentModel(
    string Title,
    string? Subtitle,
    string? Introduction,
    IReadOnlyList<NoteDocumentSection> Sections);

/*
 * Belgenin bir ana bölümünü temsil eder.
 */
public sealed record NoteDocumentSection(
    string Heading,
    IReadOnlyList<NoteDocumentBlock> Blocks);

/*
 * Bir bölüm içerisindeki içerik bloğunu
 * temsil eder.
 *
 * Desteklenen Type değerleri:
 * - paragraph
 * - definition
 * - theorem
 * - equation
 * - example
 * - solution
 * - list
 * - warning
 */
public sealed record NoteDocumentBlock(
    string Type,
    string? Heading,
    string? Text,
    string? Latex,
    IReadOnlyList<string> Items);

/*
 * Desteklenen blok türlerinin merkezi listesi.
 */
public static class NoteDocumentBlockTypes
{
    public const string Paragraph =
        "paragraph";

    public const string Definition =
        "definition";

    public const string Theorem =
        "theorem";

    public const string Equation =
        "equation";

    public const string Example =
        "example";

    public const string Solution =
        "solution";

    public const string List =
        "list";

    public const string Warning =
        "warning";

    private static readonly HashSet<string>
        SupportedTypes =
            new(
                new[]
                {
                    Paragraph,
                    Definition,
                    Theorem,
                    Equation,
                    Example,
                    Solution,
                    List,
                    Warning
                },
                StringComparer.Ordinal);

    public static bool IsSupported(
        string type)
    {
        return
            !string.IsNullOrWhiteSpace(type) &&
            SupportedTypes.Contains(type);
    }
}
