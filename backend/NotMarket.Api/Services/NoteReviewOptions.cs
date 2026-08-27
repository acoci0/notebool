namespace NotMarket.Api.Services;

public sealed class NoteReviewOptions
{
    public const string SectionName =
        "NoteReview";

    /*
     * Bu değere ulaşan notlar otomatik
     * onaylanabilir. Ayarlardan değiştirilebilir.
     */
    public int AutoApproveThreshold { get; set; } =
        85;

    public NoteReviewWeightOptions Weights { get; set; } =
        new();
}

public sealed class NoteReviewWeightOptions
{
    public int Readability { get; set; } =
        25;

    public int CourseMatch { get; set; } =
        25;

    public int DepartmentMatch { get; set; } =
        20;

    public int ContentCompleteness { get; set; } =
        20;

    public int OriginalityAndReliability { get; set; } =
        10;

    public int Total =>
        Readability +
        CourseMatch +
        DepartmentMatch +
        ContentCompleteness +
        OriginalityAndReliability;
}