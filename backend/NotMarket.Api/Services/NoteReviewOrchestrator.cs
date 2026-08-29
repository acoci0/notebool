using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NotMarket.Api.Data;
using NotMarket.Api.Domain;

namespace NotMarket.Api.Services;

public sealed class NoteReviewOrchestrator(
    AppDbContext db,
    INoteDocumentStorage storage,
    INoteReviewService reviewService,
    INotePdfGenerationQueue pdfGenerationQueue,
    IOptions<OpenAiOptions> openAiOptions)
    : INoteReviewOrchestrator
{
    private readonly OpenAiOptions _openAiOptions =
        openAiOptions.Value;

    public async Task<NoteReviewResult> ReviewAsync(
        Guid noteSubmissionId,
        CancellationToken cancellationToken)
    {
        /*
         * Notu atomik olarak AI incelemesine alır.
         * Aynı notun eş zamanlı olarak iki kez
         * işlenmesini engeller.
         */
        var claimed =
            await db.NoteSubmissions
                .Where(
                    x =>
                        x.Id ==
                            noteSubmissionId &&
                        x.Status ==
                            NoteSubmissionStatus.Uploaded)
                .ExecuteUpdateAsync(
                    setters =>
                        setters.SetProperty(
                            x => x.Status,
                            NoteSubmissionStatus.AiReview),
                    cancellationToken);

        if (claimed == 0)
        {
            var exists =
                await db.NoteSubmissions
                    .AsNoTracking()
                    .AnyAsync(
                        x =>
                            x.Id ==
                            noteSubmissionId,
                        cancellationToken);

            if (!exists)
            {
                throw new KeyNotFoundException(
                    "İncelenecek not bulunamadı.");
            }

            throw new InvalidOperationException(
                "Not AI incelemesine uygun durumda değil veya hâlihazırda işleniyor.");
        }

        try
        {
            var submission =
                await db.NoteSubmissions
                    .Include(x => x.Request)
                    .SingleAsync(
                        x =>
                            x.Id ==
                            noteSubmissionId,
                        cancellationToken);

            await using var document =
                await storage.OpenReadAsync(
                    submission.OriginalBlobPath,
                    cancellationToken);

            /*
             * Dosya bulunamazsa boş içerik gönderilir.
             * OpenAiNoteReviewService bunu teknik ret
             * olarak değerlendirecektir.
             */
            var documentBytes =
                document is null
                    ? Array.Empty<byte>()
                    : await ReadWithLimitAsync(
                        document,
                        _openAiOptions
                            .MaxDocumentBytes,
                        cancellationToken);

            var input =
                new NoteReviewInput(
                    submission.Id,
                    submission.Title,
                    submission.Request
                        .UniversityName,
                    submission.Request
                        .DepartmentName,
                    submission.Request
                        .CourseName,
                    submission.Request
                        .CriteriaJson,
                    Path.GetFileName(
                        submission.OriginalBlobPath),
                    "application/pdf",
                    documentBytes);

            var result =
                await reviewService.ReviewAsync(
                    input,
                    cancellationToken);

            var aiReview =
                new NoteAiReview
                {
                    NoteSubmissionId =
                        submission.Id,

                    IsTechnicallyValid =
                        result.Decision !=
                        NoteReviewDecision
                            .TechnicalReject,

                    ReadabilityScore =
                        result.Scores.Readability,

                    CourseMatchScore =
                        result.Scores.CourseMatch,

                    DepartmentMatchScore =
                        result.Scores.DepartmentMatch,

                    ContentCompletenessScore =
                        result.Scores
                            .ContentCompleteness,

                    OriginalityAndReliabilityScore =
                        result.Scores
                            .OriginalityAndReliability,

                    OriginalityRiskScore =
                        result.OriginalityRiskScore,

                    OverallScore =
                        result.OverallScore,

                    ConfidenceScore =
                        result.ConfidenceScore,

                    Decision =
                        result.Decision,

                    Summary =
                        result.Summary,

                    FindingsJson =
                        JsonSerializer.Serialize(
                            result.Findings),

                    DetectedCourse =
                        result.DetectedCourse,

                    DetectedDepartment =
                        result.DetectedDepartment,

                    ModelName =
                        result.ModelName,

                    PromptVersion =
                        result.PromptVersion,

                    ReviewedAt =
                        result.ReviewedAt
                };

            db.NoteAiReviews.Add(
                aiReview);

            /*
             * Eski özet puan alanları admin
             * ekranıyla geriye uyumluluk için
             * güncellenir.
             */
            submission.MatchScore =
                CalculateMatchScore(
                    result.Scores);

            submission.ReadabilityScore =
                result.Scores.Readability;

            submission.OriginalityRiskScore =
                result.OriginalityRiskScore;

            submission.ReviewNote =
                result.Summary;

            submission.ReviewedAt =
                result.ReviewedAt;

            submission.ReviewedByUserId =
                null;

            submission.Status =
                result.Decision switch
                {
                    NoteReviewDecision.AutoApprove =>
                        NoteSubmissionStatus
                            .PdfGeneration,

                    NoteReviewDecision.ManualReview =>
                        NoteSubmissionStatus
                            .ManualReview,

                    NoteReviewDecision.TechnicalReject =>
                        NoteSubmissionStatus
                            .Rejected,

                    _ =>
                        throw new InvalidOperationException(
                            "Desteklenmeyen AI inceleme kararı.")
                };

            await db.SaveChangesAsync(
                cancellationToken);

            /*
            * AI otomatik onay verdiyse not artık
            * PDF üretim kuyruğuna gönderilir.
            */
            if (
                result.Decision ==
                NoteReviewDecision.AutoApprove
            )
            {
                await pdfGenerationQueue.EnqueueAsync(
                    submission.Id,
                    cancellationToken);
            }

            return result;
        }
        catch
        {
            /*
             * API veya geçici altyapı hatasında
             * not yeniden denenebilsin diye
             * Uploaded durumuna döndürülür.
             */
            try
            {
                await db.NoteSubmissions
                    .Where(
                        x =>
                            x.Id ==
                                noteSubmissionId &&
                            x.Status ==
                                NoteSubmissionStatus
                                    .AiReview)
                    .ExecuteUpdateAsync(
                        setters =>
                            setters.SetProperty(
                                x => x.Status,
                                NoteSubmissionStatus
                                    .Uploaded),
                        CancellationToken.None);
            }
            catch
            {
                /*
                 * İlk hatanın kaybolmaması için
                 * geri alma hatası burada yutulur.
                 */
            }

            throw;
        }
    }

    private static int CalculateMatchScore(
        NoteReviewComponentScores scores)
    {
        /*
         * Ders eşleşmesi %25,
         * bölüm uyumu %20 ağırlığındadır.
         * Bu iki ölçüt kendi içerisinde
         * tekrar 0-100 aralığına çevrilir.
         */
        var score =
            (
                scores.CourseMatch * 25m +
                scores.DepartmentMatch * 20m
            )
            / 45m;

        return (int)decimal.Round(
            score,
            0,
            MidpointRounding.AwayFromZero);
    }

    private static async Task<byte[]>
        ReadWithLimitAsync(
            Stream source,
            int maximumBytes,
            CancellationToken cancellationToken)
    {
        await using var target =
            new MemoryStream();

        var buffer =
            new byte[81920];

        while (
            target.Length <=
            maximumBytes
        )
        {
            var remaining =
                maximumBytes +
                1L -
                target.Length;

            var requested =
                (int)Math.Min(
                    buffer.Length,
                    remaining);

            if (requested <= 0)
            {
                break;
            }

            var read =
                await source.ReadAsync(
                    buffer.AsMemory(
                        0,
                        requested),
                    cancellationToken);

            if (read == 0)
            {
                break;
            }

            await target.WriteAsync(
                buffer.AsMemory(
                    0,
                    read),
                cancellationToken);
        }

        return target.ToArray();
    }
}
