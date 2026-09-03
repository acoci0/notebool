using System.Threading.Channels;

namespace NotMarket.Api.Services;

public sealed class NotePdfGenerationQueue
    : INotePdfGenerationQueue
{
    private const int QueueCapacity =
        2000;

    private readonly
        Channel<NotePdfGenerationQueueItem>
        _channel =
            Channel.CreateBounded<
                NotePdfGenerationQueueItem>(
                new BoundedChannelOptions(
                    QueueCapacity)
                {
                    FullMode =
                        BoundedChannelFullMode.Wait,

                    SingleReader =
                        true,

                    SingleWriter =
                        false,

                    AllowSynchronousContinuations =
                        false
                });

    public ValueTask EnqueueAsync(
        Guid noteSubmissionId,
        CancellationToken cancellationToken)
    {
        return EnqueueAsync(
            new NotePdfGenerationQueueItem(
                noteSubmissionId,
                NotePdfGenerationMode
                    .UseCachedContent),
            cancellationToken);
    }

    public ValueTask EnqueueAsync(
        NotePdfGenerationQueueItem item,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(
            item);

        if (item.NoteSubmissionId == Guid.Empty)
        {
            throw new ArgumentException(
                "PDF üretim kuyruğu için not gönderim ID'si geçersiz.",
                nameof(item));
        }

        if (!Enum.IsDefined(item.Mode))
        {
            throw new ArgumentOutOfRangeException(
                nameof(item),
                "PDF üretim modu geçersiz.");
        }

        return _channel.Writer.WriteAsync(
            item,
            cancellationToken);
    }

    public IAsyncEnumerable<
        NotePdfGenerationQueueItem>
        ReadAllAsync(
            CancellationToken cancellationToken)
    {
        return _channel.Reader.ReadAllAsync(
            cancellationToken);
    }
}