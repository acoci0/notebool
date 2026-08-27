namespace NotMarket.Api.Services;

public sealed class NoteReviewBackgroundService(
    INoteReviewQueue queue,
    IServiceScopeFactory scopeFactory,
    ILogger<NoteReviewBackgroundService> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        logger.LogInformation(
            "Not AI inceleme arka plan servisi başlatıldı.");

        while (!stoppingToken.IsCancellationRequested)
        {
            Guid noteSubmissionId;

            try
            {
                noteSubmissionId =
                    await queue.DequeueAsync(
                        stoppingToken);
            }
            catch (OperationCanceledException)
                when (stoppingToken.IsCancellationRequested)
            {
                break;
            }

            try
            {
                await using var scope =
                    scopeFactory.CreateAsyncScope();

                var orchestrator =
                    scope.ServiceProvider
                        .GetRequiredService<
                            INoteReviewOrchestrator>();

                logger.LogInformation(
                    "Not AI incelemesi başladı. NoteSubmissionId: {NoteSubmissionId}",
                    noteSubmissionId);

                await orchestrator.ReviewAsync(
                    noteSubmissionId,
                    stoppingToken);

                logger.LogInformation(
                    "Not AI incelemesi tamamlandı. NoteSubmissionId: {NoteSubmissionId}",
                    noteSubmissionId);
            }
            catch (OperationCanceledException)
                when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(
                    exception,
                    "Not AI incelemesi başarısız oldu. NoteSubmissionId: {NoteSubmissionId}",
                    noteSubmissionId);
            }
        }

        logger.LogInformation(
            "Not AI inceleme arka plan servisi durduruldu.");
    }
}