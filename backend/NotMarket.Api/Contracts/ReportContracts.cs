namespace NotMarket.Api.Contracts;

public sealed record TrackSiteVisitRequest(
    string SessionId,
    string Path);

public sealed record AdminReportsDto(
    ReportSummaryDto Summary,
    IReadOnlyList<ReportFilterOptionDto> UniversityOptions,
    IReadOnlyList<ReportChartPointDto> Visits,
    IReadOnlyList<ReportDistributionDto> UniversityDistribution,
    IReadOnlyList<ReportDistributionDto> FacultyDistribution,
    IReadOnlyList<ReportModerationItemDto> RecentModeration,
    DateTimeOffset GeneratedAt);

public sealed record ReportFilterOptionDto(
    Guid Id,
    string Name);

public sealed record ReportSummaryDto(
    int TotalUsers,
    int VerifiedStudents,
    int PendingVerifications,
    int TotalSales,
    decimal PlatformRevenue,
    int OpenComplaints);

public sealed record ReportChartPointDto(
    string Label,
    int Value);

public sealed record ReportDistributionDto(
    string Name,
    int Count,
    int Percentage);

public sealed record ReportModerationItemDto(
    string UserEmail,
    string Type,
    string Status,
    DateTimeOffset CreatedAt);
