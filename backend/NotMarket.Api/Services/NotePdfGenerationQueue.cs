using System.Threading.Channels;

namespace NotMarket.Api.Services;

public sealed class NotePdfGenerationQueue
    : INotePdfGenerationQueue
{
    private const int QueueCapacity =
        2000;

    private readonly Channel<Guid> _channel =
        Channel.CreateBounded<Guid>(
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
        if (noteSubmissionId == Guid.Empty)
        {
            throw new ArgumentException(
                "PDF üretim kuyruğu için not gönderim ID'si geçersiz.",
                nameof(noteSubmissionId));
        }

        return _channel.Writer.WriteAsync(
            noteSubmissionId,
            cancellationToken);
    }

    public IAsyncEnumerable<Guid> ReadAllAsync(
        CancellationToken cancellationToken)
    {
        return _channel.Reader.ReadAllAsync(
            cancellationToken);
    }
}
