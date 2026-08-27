using Microsoft.Extensions.Options;
using NotMarket.Api.Domain;
namespace NotMarket.Api.Services;

/*
 * Yapay zekânın verdiği bileşen puanlarını,
 * yapılandırmadaki ağırlıklara göre tek bir
 * toplam puana ve sistem kararına dönüştürür.
 */
public sealed class NoteReviewScoreCalculator(
    IOptions<NoteReviewOptions> options)
{
    private readonly NoteReviewOptions _options =
        ValidateOptions(options.Value);

    public NoteReviewResult Calculate(
        Guid noteSubmissionId,
        AiNoteEvaluation evaluation)
    {
        ValidateScore(
            evaluation.ConfidenceScore,
            nameof(evaluation.ConfidenceScore));
        ValidateComponentScores(
            evaluation.Scores);
       
        if (!evaluation.IsTechnicallyValid)
        {
            return new NoteReviewResult(
                noteSubmissionId,
                evaluation.Scores,
                0,
                100,
                evaluation.ConfidenceScore,
                NoteReviewDecision.TechnicalReject,
                evaluation.Summary,
                evaluation.Findings ?? [],
                evaluation.DetectedCourse,
                evaluation.DetectedDepartment,
                evaluation.ModelName,
                evaluation.PromptVersion,
                DateTimeOffset.UtcNow);
        }

        ValidateComponentScores(
            evaluation.Scores);

        var weights =
            _options.Weights;

        var weightedScore =
            (
                evaluation.Scores.Readability *
                    weights.Readability +

                evaluation.Scores.CourseMatch *
                    weights.CourseMatch +

                evaluation.Scores.DepartmentMatch *
                    weights.DepartmentMatch +

                evaluation.Scores.ContentCompleteness *
                    weights.ContentCompleteness +

                evaluation.Scores.OriginalityAndReliability *
                    weights.OriginalityAndReliability
            )
            / 100m;

        var overallScore =
            (int)decimal.Round(
                weightedScore,
                0,
                MidpointRounding.AwayFromZero);

        /*
         * Mevcut veritabanı alanı risk puanı
         * tuttuğu için özgünlük puanı ters çevrilir.
         *
         * Özgünlük 90 ise risk 10 olur.
         */
        var originalityRiskScore =
            100 -
            evaluation.Scores
                .OriginalityAndReliability;

        var decision =
            overallScore >=
            _options.AutoApproveThreshold
                ? NoteReviewDecision.AutoApprove
                : NoteReviewDecision.ManualReview;

        return new NoteReviewResult(
            noteSubmissionId,
            evaluation.Scores,
            overallScore,
            originalityRiskScore,
            evaluation.ConfidenceScore,
            decision,
            evaluation.Summary,
            evaluation.Findings ?? [],
            evaluation.DetectedCourse,
            evaluation.DetectedDepartment,
            evaluation.ModelName,
            evaluation.PromptVersion,
            DateTimeOffset.UtcNow);
    }

    private static NoteReviewOptions ValidateOptions(
        NoteReviewOptions options)
    {
        if (
            options.AutoApproveThreshold is < 0 or > 100
        )
        {
            throw new InvalidOperationException(
                "Otomatik onay sınırı 0-100 arasında olmalıdır.");
        }

        if (options.Weights.Total != 100)
        {
            throw new InvalidOperationException(
                "Not inceleme ağırlıklarının toplamı 100 olmalıdır.");
        }

        ValidateScore(
            options.Weights.Readability,
            nameof(options.Weights.Readability));

        ValidateScore(
            options.Weights.CourseMatch,
            nameof(options.Weights.CourseMatch));

        ValidateScore(
            options.Weights.DepartmentMatch,
            nameof(options.Weights.DepartmentMatch));

        ValidateScore(
            options.Weights.ContentCompleteness,
            nameof(options.Weights.ContentCompleteness));

        ValidateScore(
            options.Weights.OriginalityAndReliability,
            nameof(
                options.Weights
                    .OriginalityAndReliability));

        return options;
    }

    private static void ValidateComponentScores(
        NoteReviewComponentScores scores)
    {
        ValidateScore(
            scores.Readability,
            nameof(scores.Readability));

        ValidateScore(
            scores.CourseMatch,
            nameof(scores.CourseMatch));

        ValidateScore(
            scores.DepartmentMatch,
            nameof(scores.DepartmentMatch));

        ValidateScore(
            scores.ContentCompleteness,
            nameof(scores.ContentCompleteness));

        ValidateScore(
            scores.OriginalityAndReliability,
            nameof(
                scores.OriginalityAndReliability));
    }

    private static void ValidateScore(
        int score,
        string fieldName)
    {
        if (score is < 0 or > 100)
        {
            throw new InvalidOperationException(
                $"{fieldName} değeri 0-100 arasında olmalıdır.");
        }
    }
}
