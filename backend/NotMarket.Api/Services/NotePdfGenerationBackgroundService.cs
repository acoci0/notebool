using Microsoft.EntityFrameworkCore;
using NotMarket.Api.Data;
using NotMarket.Api.Domain;

namespace NotMarket.Api.Services;

public sealed class NotePdfGenerationBackgroundService(
    INotePdfGenerationQueue queue,
    IServiceScopeFactory scopeFactory,
    ILogger<NotePdfGenerationBackgroundService> logger)
    : BackgroundService
{
    private const int MaximumRecoveredItems =
        1000;

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        logger.LogInformation(
            "Not PDF üretim arka plan servisi başlatıldı.");

        try
        {
            /*
             * Uygulama daha önce PDF üretimi
             * beklerken kapanmışsa bekleyen
             * kayıtlar tekrar kuyruğa alınır.
             */
            await RecoverPendingItemsAsync(
                stoppingToken);

            await foreach (
                var noteSubmissionId
                in queue.ReadAllAsync(
                    stoppingToken)
            )
            {
                try
                {
                    await using var scope =
                        scopeFactory
                            .CreateAsyncScope();

                    var orchestrator =
                        scope.ServiceProvider
                            .GetRequiredService<
                                INotePdfGenerationOrchestrator>();

                    logger.LogInformation(
                        "Not PDF üretimi başladı. NoteSubmissionId: {NoteSubmissionId}",
                        noteSubmissionId);

                    var result =
                        await orchestrator
                            .GenerateAsync(
                                noteSubmissionId,
                                stoppingToken);

                    logger.LogInformation(
                        "Not PDF üretimi tamamlandı. " +
                        "NoteSubmissionId: {NoteSubmissionId}, " +
                        "GeneratedPdfBytes: {GeneratedPdfBytes}, " +
                        "Compiler: {CompilerName}",
                        result.NoteSubmissionId,
                        result.GeneratedPdfBytes,
                        result.CompilerName);
                }
                catch (OperationCanceledException)
                    when (
                        stoppingToken
                            .IsCancellationRequested
                    )
                {
                    break;
                }
                catch (Exception exception)
                {
                    logger.LogError(
                        exception,
                        "Not PDF üretimi başarısız oldu. NoteSubmissionId: {NoteSubmissionId}",
                        noteSubmissionId);
                }
            }
        }
        catch (OperationCanceledException)
            when (
                stoppingToken
                    .IsCancellationRequested
            )
        {
            /*
             * Uygulamanın normal kapanışı.
             */
        }
        finally
        {
            logger.LogInformation(
                "Not PDF üretim arka plan servisi durduruldu.");
        }
    }

    private async Task RecoverPendingItemsAsync(
        CancellationToken cancellationToken)
    {
        await using var scope =
            scopeFactory.CreateAsyncScope();

        var db =
            scope.ServiceProvider
                .GetRequiredService<
                    AppDbContext>();

        /*
         * PdfGeneration:
         * Kuyruğa alınmış fakat uygulama
         * kapanmış olabilir.
         *
         * PdfGenerating:
         * İşlem sırasında uygulama kapanmış
         * olabilir. Önce tekrar bekleyen
         * duruma alınır.
         */
        await db.NoteSubmissions
            .Where(
                x =>
                    x.Status ==
                    NoteSubmissionStatus
                        .PdfGenerating)
            .ExecuteUpdateAsync(
                setters =>
                    setters
                        .SetProperty(
                            x => x.Status,
                            NoteSubmissionStatus
                                .PdfGeneration)
                        .SetProperty(
                            x => x.PdfGenerationError,
                            "Uygulama PDF üretimi sırasında yeniden başlatıldı."),
                cancellationToken);

        var pendingIds =
            await db.NoteSubmissions
                .AsNoTracking()
                .Where(
                    x =>
                        x.Status ==
                        NoteSubmissionStatus
                            .PdfGeneration)
                .OrderBy(
                    x => x.CreatedAt)
                .Take(
                    MaximumRecoveredItems)
                .Select(
                    x => x.Id)
                .ToListAsync(
                    cancellationToken);

        foreach (var noteSubmissionId
                 in pendingIds)
        {
            await queue.EnqueueAsync(
                noteSubmissionId,
                cancellationToken);
        }

        if (pendingIds.Count > 0)
        {
            logger.LogInformation(
                "{Count} adet bekleyen not PDF üretim kuyruğuna geri alındı.",
                pendingIds.Count);
        }
    }
}
