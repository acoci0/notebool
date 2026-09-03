namespace NotMarket.Api.Services;

/*
 * PDF üretiminin önbellekten mi yoksa
 * OpenAI dönüşümü yenilenerek mi
 * gerçekleştirileceğini belirtir.
 */
public enum NotePdfGenerationMode
{
    UseCachedContent = 0,
    RegenerateContent = 1
}

/*
 * Arka plan kuyruğunda taşınan PDF
 * üretim isteği.
 */
public sealed record NotePdfGenerationQueueItem(
    Guid NoteSubmissionId,
    NotePdfGenerationMode Mode);

/*
 * PDF üretilecek notların uygulama içi
 * kuyruğunu temsil eder.
 */
public interface INotePdfGenerationQueue
{
    /*
     * Mevcut çağrılar için varsayılan olarak
     * önbelleği kullanan üretim isteği oluşturur.
     */
    ValueTask EnqueueAsync(
        Guid noteSubmissionId,
        CancellationToken cancellationToken);

    /*
     * Üretim modu açıkça belirtilmiş bir
     * isteği kuyruğa ekler.
     */
    ValueTask EnqueueAsync(
        NotePdfGenerationQueueItem item,
        CancellationToken cancellationToken);

    IAsyncEnumerable<NotePdfGenerationQueueItem>
        ReadAllAsync(
            CancellationToken cancellationToken);
}