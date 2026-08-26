using System.ComponentModel.DataAnnotations;

namespace NotMarket.Api.Domain;

public sealed class Payment
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid OrderId { get; set; }
    public Order Order { get; set; } = null!;

    [MaxLength(50)]
    public required string Provider { get; set; }

    [MaxLength(200)]
    public string? ProviderPaymentId { get; set; }

    public decimal Amount { get; set; }

    [MaxLength(3)]
    public string Currency { get; set; } = "TRY";

    public PaymentStatus Status { get; set; } =
        PaymentStatus.Pending;

    [MaxLength(600)]
    public string? FailureReason { get; set; }

    public DateTimeOffset CreatedAt { get; set; } =
        DateTimeOffset.UtcNow;

    public DateTimeOffset? CompletedAt { get; set; }
}