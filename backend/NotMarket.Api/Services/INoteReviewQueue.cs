using System.Threading.Channels;

namespace NotMarket.Api.Services;

public interface INoteReviewQueue
{
    ValueTask EnqueueAsync(
        Guid noteSubmissionId,
        CancellationToken cancellationToken);

    ValueTask<Guid> DequeueAsync(
        CancellationToken cancellationToken);
}

public sealed class NoteReviewQueue : INoteReviewQueue
{
    private readonly Channel<Guid> _channel =
        Channel.CreateBounded<Guid>(
            new BoundedChannelOptions(100)
            {
                FullMode =
                    BoundedChannelFullMode.Wait,

                SingleReader =
                    true,

                SingleWriter =
                    false
            });

    public ValueTask EnqueueAsync(
        Guid noteSubmissionId,
        CancellationToken cancellationToken)
    {
        if (noteSubmissionId == Guid.Empty)
        {
            throw new ArgumentException(
                "Not gönderimi ID değeri boş olamaz.",
                nameof(noteSubmissionId));
        }

        return _channel.Writer.WriteAsync(
            noteSubmissionId,
            cancellationToken);
    }

    public ValueTask<Guid> DequeueAsync(
        CancellationToken cancellationToken)
    {
        return _channel.Reader.ReadAsync(
            cancellationToken);
    }
}