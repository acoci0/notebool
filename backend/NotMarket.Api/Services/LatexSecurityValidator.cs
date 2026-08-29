using System.Text.RegularExpressions;

namespace NotMarket.Api.Services;

/*
 * OpenAI tarafından yalnızca denklem blokları
 * için üretilen LaTeX içeriğini denetler.
 *
 * Tam belge, dosya erişimi, komut tanımlama,
 * dış kaynak veya shell erişimi sağlayabilecek
 * yapılar reddedilir.
 */
public sealed class LatexSecurityValidator
{
    private static readonly Regex
        ForbiddenCommandRegex =
            new(
                """
                \\(?:
                    documentclass|
                    usepackage|
                    input|
                    include|
                    includeonly|
                    filecontents|
                    write|
                    write18|
                    openout|
                    closeout|
                    openin|
                    closein|
                    read|
                    newread|
                    newwrite|
                    immediate|
                    catcode|
                    newcommand|
                    renewcommand|
                    providecommand|
                    newenvironment|
                    renewenvironment|
                    def|
                    edef|
                    gdef|
                    xdef|
                    special|
                    csname|
                    endcsname|
                    loop|
                    repeat|
                    directlua|
                    latelua|
                    pdfobj|
                    pdffile|
                    pdfximage|
                    pdfcatalog|
                    pdfannot|
                    pdfstartlink|
                    everyjob|
                    everyeof|
                    jobname|
                    bibliography|
                    addbibresource|
                    lstinputlisting|
                    verbatiminput|
                    includegraphics|
                    graphicspath|
                    href|
                    url|
                    AtBeginDocument|
                    AtEndDocument|
                    ExplSyntaxOn|
                    ExplSyntaxOff|
                    ShellEscape
                )\b
                """,
                RegexOptions.IgnoreCase |
                RegexOptions.Compiled |
                RegexOptions.IgnorePatternWhitespace |
                RegexOptions.CultureInvariant);

    private static readonly Regex
        EnvironmentRegex =
            new(
                """
                \\(?<action>begin|end)
                \s*
                \{
                    (?<name>[^{}]+)
                \}
                """,
                RegexOptions.IgnoreCase |
                RegexOptions.Compiled |
                RegexOptions.IgnorePatternWhitespace |
                RegexOptions.CultureInvariant);

    private static readonly HashSet<string>
        AllowedEnvironments =
            new(
                new[]
                {
                    "aligned",
                    "alignedat",
                    "array",
                    "cases",
                    "matrix",
                    "pmatrix",
                    "bmatrix",
                    "Bmatrix",
                    "vmatrix",
                    "Vmatrix",
                    "smallmatrix",
                    "gathered",
                    "split"
                },
                StringComparer.Ordinal);

    public void ValidateDocument(
        NoteDocumentModel document)
    {
        ArgumentNullException.ThrowIfNull(
            document);

        if (document.Sections is null)
        {
            throw new InvalidOperationException(
                "LaTeX güvenlik kontrolü için belge bölümleri bulunamadı.");
        }

        foreach (var section
                 in document.Sections)
        {
            if (section.Blocks is null)
            {
                throw new InvalidOperationException(
                    "LaTeX güvenlik kontrolü için bölüm içeriği bulunamadı.");
            }

            foreach (var block
                     in section.Blocks)
            {
                if (
                    block.Type ==
                    NoteDocumentBlockTypes.Equation
                )
                {
                    ValidateEquation(
                        block.Latex);
                }
                else if (
                    !string.IsNullOrWhiteSpace(
                        block.Latex)
                )
                {
                    throw new InvalidOperationException(
                        "LaTeX içeriği yalnızca equation bloklarında kullanılabilir.");
                }
            }
        }
    }

    public void ValidateEquation(
        string? latex)
    {
        if (string.IsNullOrWhiteSpace(latex))
        {
            throw new InvalidOperationException(
                "Denklem LaTeX içeriği boş.");
        }

        if (latex.Length > 10000)
        {
            throw new InvalidOperationException(
                "Denklem LaTeX içeriği izin verilen uzunluğu aşıyor.");
        }

        if (
            latex.Contains(
                '\0')
        )
        {
            throw new InvalidOperationException(
                "Denklem LaTeX içeriğinde geçersiz karakter bulunuyor.");
        }

        if (
            latex.Any(
                character =>
                    char.IsControl(character) &&
                    character is not
                        '\r' and
                        '\n' and
                        '\t')
        )
        {
            throw new InvalidOperationException(
                "Denklem LaTeX içeriğinde kontrol karakteri bulunuyor.");
        }

        /*
         * TeX'in ^^ karakter kodlama özelliği,
         * yasaklı komut kontrollerini aşmak için
         * kullanılabileceğinden engellenir.
         */
        if (
            latex.Contains(
                "^^",
                StringComparison.Ordinal)
        )
        {
            throw new InvalidOperationException(
                "Denklem LaTeX içeriğinde izin verilmeyen karakter kodlaması bulunuyor.");
        }

        /*
         * Denklem zaten backend tarafından
         * display-math ortamına alınacaktır.
         */
        if (
            latex.Contains(
                '$') ||
            latex.Contains(
                @"\[",
                StringComparison.Ordinal) ||
            latex.Contains(
                @"\]",
                StringComparison.Ordinal)
        )
        {
            throw new InvalidOperationException(
                "Denklem içeriği matematik ortamı belirteci içermemelidir.");
        }

        /*
         * Yorum karakteri kapanış komutlarını
         * etkisizleştirebileceği için engellenir.
         */
        if (
            latex.Contains(
                '%')
        )
        {
            throw new InvalidOperationException(
                "Denklem LaTeX içeriğinde yorum karakteri kullanılamaz.");
        }

        if (
            ForbiddenCommandRegex.IsMatch(
                latex)
        )
        {
            throw new InvalidOperationException(
                "Denklem LaTeX içeriğinde güvenli olmayan komut bulunuyor.");
        }

        ValidateBalancedBraces(
            latex);

        ValidateEnvironments(
            latex);
    }

    private static void ValidateBalancedBraces(
        string latex)
    {
        var depth =
            0;

        for (
            var index = 0;
            index < latex.Length;
            index++
        )
        {
            var character =
                latex[index];

            if (
                character is not
                    '{' and not
                    '}'
            )
            {
                continue;
            }

            if (
                IsEscaped(
                    latex,
                    index)
            )
            {
                continue;
            }

            if (character == '{')
            {
                depth++;
                continue;
            }

            depth--;

            if (depth < 0)
            {
                throw new InvalidOperationException(
                    "Denklem LaTeX içeriğinde dengesiz süslü parantez bulunuyor.");
            }
        }

        if (depth != 0)
        {
            throw new InvalidOperationException(
                "Denklem LaTeX içeriğinde dengesiz süslü parantez bulunuyor.");
        }
    }

    private static void ValidateEnvironments(
        string latex)
    {
        var environmentStack =
            new Stack<string>();

        foreach (
            Match match
            in EnvironmentRegex.Matches(
                latex)
        )
        {
            var action =
                match.Groups["action"]
                    .Value;

            var environment =
                match.Groups["name"]
                    .Value
                    .Trim();

            if (
                !AllowedEnvironments.Contains(
                    environment)
            )
            {
                throw new InvalidOperationException(
                    $"İzin verilmeyen LaTeX ortamı: {environment}");
            }

            if (
                string.Equals(
                    action,
                    "begin",
                    StringComparison.OrdinalIgnoreCase)
            )
            {
                environmentStack.Push(
                    environment);

                continue;
            }

            if (
                environmentStack.Count == 0
            )
            {
                throw new InvalidOperationException(
                    "LaTeX ortamı geçersiz sırada kapatılmış.");
            }

            var expectedEnvironment =
                environmentStack.Pop();

            if (
                !string.Equals(
                    expectedEnvironment,
                    environment,
                    StringComparison.Ordinal)
            )
            {
                throw new InvalidOperationException(
                    "LaTeX ortamlarının açılış ve kapanış sırası geçersiz.");
            }
        }

        if (environmentStack.Count != 0)
        {
            throw new InvalidOperationException(
                "Kapatılmamış LaTeX ortamı bulunuyor.");
        }
    }

    private static bool IsEscaped(
        string value,
        int characterIndex)
    {
        var precedingBackslashCount =
            0;

        for (
            var index =
                characterIndex - 1;
            index >= 0 &&
            value[index] == '\\';
            index--
        )
        {
            precedingBackslashCount++;
        }

        return
            precedingBackslashCount % 2 != 0;
    }
}
