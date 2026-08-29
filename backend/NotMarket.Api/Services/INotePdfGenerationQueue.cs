namespace NotMarket.Api.Services;

/*
 * PDF üretilecek notların uygulama içi
 * kuyruğunu temsil eder.
 */
public interface INotePdfGenerationQueue
{
    ValueTask EnqueueAsync(
        Guid noteSubmissionId,
        CancellationToken cancellationToken);

    IAsyncEnumerable<Guid> ReadAllAsync(
        CancellationToken cancellationToken);
}