namespace NotMarket.Api.Contracts;

public sealed record CreateOrderRequest(
    Guid NoteSubmissionId);

public sealed record OrderResponse(
    Guid Id,
    Guid NoteSubmissionId,
    string NoteTitle,
    decimal Amount,
    string Currency,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? PaidAt,
    DateTimeOffset? CancelledAt);