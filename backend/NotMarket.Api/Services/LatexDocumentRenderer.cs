using System.Globalization;
using System.Text;
using Microsoft.Extensions.Options;

namespace NotMarket.Api.Services;

public sealed class LatexDocumentRenderer(
    LatexSecurityValidator securityValidator,
    IOptions<NotePdfGenerationOptions> options)
    : ILatexDocumentRenderer
{
    private const string DocumentPreamble =
        """
        \documentclass[11pt,a4paper]{article}

        \usepackage{fontspec}
        \usepackage{unicode-math}
        \usepackage{geometry}
        \usepackage{xcolor}
        \usepackage{amsmath}
        \usepackage{amssymb}
        \usepackage{mathtools}
        \usepackage{enumitem}
        \usepackage[most]{tcolorbox}
        \usepackage{fancyhdr}
        \usepackage{titlesec}
        \usepackage{hyperref}
        \usepackage{microtype}

        \setmainfont{Latin Modern Roman}
        \setmathfont{Latin Modern Math}

        \geometry{
          top=22mm,
          bottom=22mm,
          left=24mm,
          right=24mm,
          headheight=15pt
        }

        \definecolor{NotMarketPrimary}{HTML}{6D214F}
        \definecolor{NotMarketSecondary}{HTML}{A23B72}
        \definecolor{NotMarketLight}{HTML}{F8EEF4}
        \definecolor{NotMarketBlue}{HTML}{EAF2FF}
        \definecolor{NotMarketGreen}{HTML}{EAF8F0}
        \definecolor{NotMarketYellow}{HTML}{FFF8DC}
        \definecolor{NotMarketText}{HTML}{252525}

        \hypersetup{
          unicode=true,
          colorlinks=true,
          linkcolor=NotMarketPrimary,
          urlcolor=NotMarketSecondary,
          pdftitle={NotMarket Ders Notu},
          pdfauthor={NotMarket}
        }

        \pagestyle{fancy}
        \fancyhf{}
        \fancyhead[L]{\textcolor{NotMarketPrimary}{NotMarket}}
        \fancyhead[R]{\textcolor{NotMarketPrimary}{Ders Notu}}
        \fancyfoot[C]{\thepage}

        \titleformat{\section}
          {\Large\bfseries\color{NotMarketPrimary}}
          {\thesection}
          {0.7em}
          {}

        \titleformat{\subsection}
          {\large\bfseries\color{NotMarketSecondary}}
          {\thesubsection}
          {0.7em}
          {}

        \setlength{\parindent}{0pt}
        \setlength{\parskip}{0.75em}
        \setlist[itemize]{
          leftmargin=1.8em,
          itemsep=0.35em,
          topsep=0.4em
        }

        \renewcommand{\contentsname}{İçindekiler}

        \newtcolorbox{definitionbox}[1][]{
          enhanced,
          breakable,
          colback=NotMarketLight,
          colframe=NotMarketPrimary,
          boxrule=0.7pt,
          arc=2mm,
          title={#1},
          fonttitle=\bfseries
        }

        \newtcolorbox{theorembox}[1][]{
          enhanced,
          breakable,
          colback=NotMarketBlue,
          colframe=blue!55!black,
          boxrule=0.7pt,
          arc=2mm,
          title={#1},
          fonttitle=\bfseries
        }

        \newtcolorbox{examplebox}[1][]{
          enhanced,
          breakable,
          colback=NotMarketGreen,
          colframe=green!45!black,
          boxrule=0.7pt,
          arc=2mm,
          title={#1},
          fonttitle=\bfseries
        }

        \newtcolorbox{solutionbox}[1][]{
          enhanced,
          breakable,
          colback=white,
          colframe=NotMarketSecondary,
          boxrule=0.7pt,
          arc=2mm,
          title={#1},
          fonttitle=\bfseries
        }

        \newtcolorbox{warningbox}[1][]{
          enhanced,
          breakable,
          colback=NotMarketYellow,
          colframe=orange!70!black,
          boxrule=0.7pt,
          arc=2mm,
          title={#1},
          fonttitle=\bfseries
        }

        \begin{document}
        """;

    private readonly NotePdfGenerationOptions
        _options =
            options.Value;

    public LatexDocumentRenderResult Render(
        LatexDocumentRenderInput input)
    {
        ArgumentNullException.ThrowIfNull(
            input);

        ValidateMetadata(
            input.Metadata);

        ArgumentNullException.ThrowIfNull(
            input.Document);

        securityValidator.ValidateDocument(
            input.Document);

        var source =
            new StringBuilder(
                capacity: 32768);

        source.AppendLine(
            DocumentPreamble);

        AppendTitlePage(
            source,
            input.Metadata,
            input.Document);

        if (
            !string.IsNullOrWhiteSpace(
                input.Document.Introduction)
        )
        {
            source.AppendLine(
                @"\section*{Ders Notu Hakkında}");

            source.AppendLine(
                EscapeText(
                    input.Document
                        .Introduction));

            source.AppendLine();
        }

        source.AppendLine(
            @"\tableofcontents");

        source.AppendLine(
            @"\newpage");

        foreach (var section
                 in input.Document.Sections)
        {
            AppendSection(
                source,
                section);
        }

        source.AppendLine(
            @"\end{document}");

        var latexSource =
            source.ToString();

        if (
            latexSource.Length >
            _options.MaxSourceCharacters
        )
        {
            throw new InvalidOperationException(
                "Oluşturulan LaTeX kaynağı izin verilen uzunluğu aşıyor.");
        }

        return new LatexDocumentRenderResult(
            latexSource,
            _options.TemplateVersion);
    }

    private static void AppendTitlePage(
        StringBuilder source,
        LatexDocumentMetadata metadata,
        NoteDocumentModel document)
    {
        source.AppendLine(
            @"\begin{titlepage}");

        source.AppendLine(
            @"\centering");

        source.AppendLine(
            @"\vspace*{2cm}");

        source.AppendLine(
            @"{\Large\bfseries\color{NotMarketPrimary} NotMarket\par}");

        source.AppendLine(
            @"\vspace{1.5cm}");

        source.Append(
            @"{\Huge\bfseries ");

        source.Append(
            EscapeText(
                document.Title));

        source.AppendLine(
            @"\par}");

        if (
            !string.IsNullOrWhiteSpace(
                document.Subtitle)
        )
        {
            source.AppendLine(
                @"\vspace{0.7cm}");

            source.Append(
                @"{\Large\color{NotMarketSecondary} ");

            source.Append(
                EscapeText(
                    document.Subtitle));

            source.AppendLine(
                @"\par}");
        }

        source.AppendLine(
            @"\vfill");

        AppendTitleMetadataLine(
            source,
            "Üniversite",
            metadata.UniversityName);

        AppendTitleMetadataLine(
            source,
            "Bölüm/Program",
            metadata.DepartmentName);

        AppendTitleMetadataLine(
            source,
            "Ders",
            metadata.CourseName);

        AppendTitleMetadataLine(
            source,
            "Not Başlığı",
            metadata.Title);

        AppendTitleMetadataLine(
            source,
            "Hazırlanma Tarihi",
            metadata.GeneratedAt
                .UtcDateTime
                .ToString(
                    "dd.MM.yyyy",
                    CultureInfo.InvariantCulture));

        source.AppendLine(
            @"\vfill");

        source.AppendLine(
            @"{\small Bu belge NotMarket belge üretim sistemi tarafından düzenlenmiştir.\par}");

        source.AppendLine(
            @"\end{titlepage}");

        source.AppendLine(
            @"\newpage");
    }

    private static void AppendTitleMetadataLine(
        StringBuilder source,
        string label,
        string value)
    {
        source.Append(
            @"{\large\textbf{");

        source.Append(
            EscapeText(
                label));

        source.Append(
            @":} ");

        source.Append(
            EscapeText(
                value));

        source.AppendLine(
            @"\par}");

        source.AppendLine(
            @"\vspace{0.25cm}");
    }

    private static void AppendSection(
        StringBuilder source,
        NoteDocumentSection section)
    {
        source.Append(
            @"\section{");

        source.Append(
            EscapeText(
                section.Heading));

        source.AppendLine(
            "}");

        foreach (var block
                 in section.Blocks)
        {
            AppendBlock(
                source,
                block);
        }
    }

    private static void AppendBlock(
        StringBuilder source,
        NoteDocumentBlock block)
    {
        switch (block.Type)
        {
            case NoteDocumentBlockTypes.Paragraph:
                AppendOptionalHeading(
                    source,
                    block.Heading);

                AppendPlainText(
                    source,
                    block.Text);

                break;

            case NoteDocumentBlockTypes.Definition:
                AppendBox(
                    source,
                    "definitionbox",
                    block.Heading ??
                    "Tanım",
                    block.Text);

                break;

            case NoteDocumentBlockTypes.Theorem:
                AppendBox(
                    source,
                    "theorembox",
                    block.Heading ??
                    "Teorem",
                    block.Text);

                break;

            case NoteDocumentBlockTypes.Equation:
                AppendOptionalHeading(
                    source,
                    block.Heading);

                source.AppendLine(
                    @"\[");

                source.AppendLine(
                    block.Latex!.Trim());

                source.AppendLine(
                    @"\]");

                source.AppendLine();

                break;

            case NoteDocumentBlockTypes.Example:
                AppendBox(
                    source,
                    "examplebox",
                    block.Heading ??
                    "Örnek",
                    block.Text);

                break;

            case NoteDocumentBlockTypes.Solution:
                AppendBox(
                    source,
                    "solutionbox",
                    block.Heading ??
                    "Çözüm",
                    block.Text);

                break;

            case NoteDocumentBlockTypes.List:
                AppendOptionalHeading(
                    source,
                    block.Heading);

                AppendList(
                    source,
                    block.Items);

                break;

            case NoteDocumentBlockTypes.Warning:
                AppendBox(
                    source,
                    "warningbox",
                    block.Heading ??
                    "Önemli Not",
                    block.Text);

                break;

            default:
                throw new InvalidOperationException(
                    $"Desteklenmeyen belge bloğu: {block.Type}");
        }
    }

    private static void AppendOptionalHeading(
        StringBuilder source,
        string? heading)
    {
        if (
            string.IsNullOrWhiteSpace(
                heading)
        )
        {
            return;
        }

        source.Append(
            @"\subsection*{");

        source.Append(
            EscapeText(
                heading));

        source.AppendLine(
            "}");
    }

    private static void AppendPlainText(
        StringBuilder source,
        string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        source.AppendLine(
            EscapeText(
                text));

        source.AppendLine();
    }

    private static void AppendBox(
        StringBuilder source,
        string environment,
        string title,
        string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        source.Append(
            @"\begin{");

        source.Append(
            environment);

        source.Append(
            "}[{");

        source.Append(
            EscapeText(
                title));

        source.AppendLine(
            "}]");

        source.AppendLine(
            EscapeText(
                text));

        source.Append(
            @"\end{");

        source.Append(
            environment);

        source.AppendLine(
            "}");

        source.AppendLine();
    }

    private static void AppendList(
        StringBuilder source,
        IReadOnlyList<string> items)
    {
        source.AppendLine(
            @"\begin{itemize}");

        foreach (var item in items)
        {
            source.Append(
                @"\item ");

            source.AppendLine(
                EscapeText(
                    item));
        }

        source.AppendLine(
            @"\end{itemize}");

        source.AppendLine();
    }

    private static string EscapeText(
        string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        var escaped =
            new StringBuilder(
                value.Length + 32);

        foreach (var character in value)
        {
            switch (character)
            {
                case '\\':
                    escaped.Append(
                        @"\textbackslash{}");
                    break;

                case '{':
                    escaped.Append(
                        @"\{");
                    break;

                case '}':
                    escaped.Append(
                        @"\}");
                    break;

                case '$':
                    escaped.Append(
                        @"\$");
                    break;

                case '&':
                    escaped.Append(
                        @"\&");
                    break;

                case '#':
                    escaped.Append(
                        @"\#");
                    break;

                case '_':
                    escaped.Append(
                        @"\_");
                    break;

                case '%':
                    escaped.Append(
                        @"\%");
                    break;

                case '~':
                    escaped.Append(
                        @"\textasciitilde{}");
                    break;

                case '^':
                    escaped.Append(
                        @"\textasciicircum{}");
                    break;

                case '\r':
                    break;

                case '\n':
                    escaped.AppendLine();
                    escaped.AppendLine();
                    break;

                default:
                    escaped.Append(
                        character);
                    break;
            }
        }

        return escaped.ToString();
    }

    private static void ValidateMetadata(
        LatexDocumentMetadata metadata)
    {
        ArgumentNullException.ThrowIfNull(
            metadata);

        if (
            metadata.NoteSubmissionId ==
            Guid.Empty
        )
        {
            throw new InvalidOperationException(
                "LaTeX belgesi için not gönderim ID'si geçersiz.");
        }

        if (
            string.IsNullOrWhiteSpace(
                metadata.Title) ||
            string.IsNullOrWhiteSpace(
                metadata.UniversityName) ||
            string.IsNullOrWhiteSpace(
                metadata.DepartmentName) ||
            string.IsNullOrWhiteSpace(
                metadata.CourseName)
        )
        {
            throw new InvalidOperationException(
                "LaTeX belge bilgileri eksik.");
        }
    }
}
