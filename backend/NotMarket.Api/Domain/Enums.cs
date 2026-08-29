namespace NotMarket.Api.Domain;

public enum UserRole
{
    Student = 1,
    Admin = 2
}

public enum AccountStatus
{
    Active = 1,
    Suspended = 2,
    Closed = 3
}

public enum VerificationStatus
{
    Pending = 1,
    Approved = 2,
    Rejected = 3,
    Expired = 4
}

public enum NoteSubmissionStatus
{
    Uploaded = 1,
    AiReview = 2,
    ManualReview = 3,
    Approved = 4,
    Rejected = 5,
    PdfGeneration = 6,
    PdfGenerating = 7,
    PdfGenerationFailed = 8
}

public enum AcademicUnitType
{
    Faculty,
    Institute,
    School,
    Conservatory,
    VocationalSchool,
    Other
}

public enum OrderStatus
{
    PendingPayment = 1,
    Paid = 2,
    PaymentFailed = 3,
    Cancelled = 4,
    Refunded = 5
}

public enum PaymentStatus
{
    Pending = 1,
    Succeeded = 2,
    Failed = 3,
    Refunded = 4
}
public enum NoteReviewDecision
{
    AutoApprove = 1,
    ManualReview = 2,
    TechnicalReject = 3
}