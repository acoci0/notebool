namespace NotMarket.Api.Services;

public interface INoteReviewOrchestrator
{
    Task<NoteReviewResult> ReviewAsync(
        Guid noteSubmissionId,
        CancellationToken cancellationToken);
}