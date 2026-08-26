using System.ComponentModel.DataAnnotations;

namespace NotMarket.Api.Domain;

public sealed class Order
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid BuyerId { get; set; }
    public ApplicationUser Buyer { get; set; } = null!;

    public Guid SellerId { get; set; }
    public ApplicationUser Seller { get; set; } = null!;

    public Guid NoteSubmissionId { get; set; }
    public NoteSubmission NoteSubmission { get; set; } = null!;

    [MaxLength(220)]
    public required string NoteTitleSnapshot { get; set; }

    public decimal GrossAmount { get; set; }

    public decimal PlatformCommissionAmount { get; set; }

    public decimal SellerEarningAmount { get; set; }

    [MaxLength(3)]
    public string Currency { get; set; } = "TRY";

    public OrderStatus Status { get; set; } =
        OrderStatus.PendingPayment;

    public DateTimeOffset CreatedAt { get; set; } =
        DateTimeOffset.UtcNow;

    public DateTimeOffset? PaidAt { get; set; }

    public DateTimeOffset? CancelledAt { get; set; }

    public Payment? Payment { get; set; }
}