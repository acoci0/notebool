using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NotMarket.Api.Contracts;
using NotMarket.Api.Data;
using NotMarket.Api.Domain;

namespace NotMarket.Api.Controllers;

[ApiController]
[Route("api/admin/reports")]
[Authorize(Policy = "AdminOnly")]
public sealed class AdminReportsController(AppDbContext db)
    : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<AdminReportsDto>> Get(
        [FromQuery] string range = "monthly",
        [FromQuery] Guid? universityId = null,
        CancellationToken cancellationToken = default)
    {
        var selectedRange = ParseRange(range);
        var now = DateTimeOffset.UtcNow;

        var approvedMemberships =
            await db.StudentVerifications
                .AsNoTracking()
                .Where(
                    x =>
                        x.Status == VerificationStatus.Approved &&
                        (
                            x.ExpiresAt == null ||
                            x.ExpiresAt > now
                        ))
                .Select(
                    x => new
                    {
                        x.UserId,
                        x.UniversityId,
                        x.UniversityName,
                        x.FacultyName
                    })
                .ToListAsync(cancellationToken);

        var universityOptions = approvedMemberships
            .Where(x => x.UniversityId.HasValue)
            .GroupBy(
                x => new
                {
                    Id = x.UniversityId!.Value,
                    x.UniversityName
                })
            .OrderBy(x => x.Key.UniversityName)
            .Select(
                x => new ReportFilterOptionDto(
                    x.Key.Id,
                    x.Key.UniversityName))
            .ToArray();

        if (
            universityId.HasValue &&
            universityOptions.All(
                x => x.Id != universityId.Value))
        {
            return BadRequest(new
            {
                message =
                    "Seçilen üniversite için aktif doğrulama kaydı bulunamadı."
            });
        }

        var filteredMemberships = universityId.HasValue
            ? approvedMemberships
                .Where(
                    x => x.UniversityId == universityId.Value)
                .ToArray()
            : approvedMemberships.ToArray();

        var totalUsers = universityId.HasValue
            ? filteredMemberships
                .Select(x => x.UserId)
                .Distinct()
                .Count()
            : await db.Users.CountAsync(cancellationToken);

        var pendingQuery = db.StudentVerifications
            .AsNoTracking()
            .Where(
                x => x.Status == VerificationStatus.Pending);

        if (universityId.HasValue)
        {
            pendingQuery = pendingQuery.Where(
                x => x.UniversityId == universityId.Value);
        }

        var pendingVerifications = await pendingQuery
            .CountAsync(cancellationToken);

        var verifiedStudents = filteredMemberships
            .Select(x => x.UserId)
            .Distinct()
            .Count();

        var universityDistribution =
            BuildDistribution(
                filteredMemberships
                    .Where(
                        x => !string.IsNullOrWhiteSpace(
                            x.UniversityName))
                    .Select(
                        x => new MembershipRow(
                            x.UserId,
                            x.UniversityName)))
                .Take(5)
                .ToArray();

        var facultyDistribution =
            BuildDistribution(
                filteredMemberships
                    .Where(
                        x => !string.IsNullOrWhiteSpace(
                            x.FacultyName))
                    .Select(
                        x => new MembershipRow(
                            x.UserId,
                            x.FacultyName)))
                .Take(5)
                .ToArray();

        var visitStart = GetVisitStart(
            selectedRange,
            now);

        var visitDatesQuery = db.SiteVisits
            .AsNoTracking()
            .Select(x => x.VisitedAt);

        if (visitStart.HasValue)
        {
            visitDatesQuery = visitDatesQuery
                .Where(
                    visitedAt =>
                        visitedAt >= visitStart.Value);
        }

        var visitDates = await visitDatesQuery
            .ToListAsync(cancellationToken);

        var visits = BuildVisitSeries(
            selectedRange,
            now,
            visitDates);

        var moderationQuery = db.StudentVerifications
            .AsNoTracking()
            .Include(x => x.User)
            .AsQueryable();

        if (universityId.HasValue)
        {
            moderationQuery = moderationQuery.Where(
                x => x.UniversityId == universityId.Value);
        }

        var moderationRows = await moderationQuery
            .OrderByDescending(x => x.CreatedAt)
            .Take(5)
            .ToListAsync(cancellationToken);

        var recentModeration = moderationRows
            .Select(
                x => new ReportModerationItemDto(
                    x.User.Email,
                    "Öğrenci Doğrulama",
                    GetStatusLabel(x.Status),
                    x.CreatedAt))
            .ToArray();

        return Ok(new AdminReportsDto(
            new ReportSummaryDto(
                totalUsers,
                verifiedStudents,
                pendingVerifications,
                TotalSales: 0,
                PlatformRevenue: 0,
                OpenComplaints: 0),
            universityOptions,
            visits,
            universityDistribution,
            facultyDistribution,
            recentModeration,
            now));
    }

    private static ReportRange ParseRange(
        string? range)
    {
        return range?.Trim().ToLowerInvariant() switch
        {
            "daily" => ReportRange.Daily,
            "weekly" => ReportRange.Weekly,
            "halfyear" => ReportRange.HalfYear,
            "yearly" => ReportRange.Yearly,
            "all" => ReportRange.All,
            _ => ReportRange.Monthly
        };
    }

    private static DateTimeOffset? GetVisitStart(
        ReportRange range,
        DateTimeOffset now)
    {
        var today = new DateTimeOffset(
            now.Year,
            now.Month,
            now.Day,
            0,
            0,
            0,
            TimeSpan.Zero);

        return range switch
        {
            ReportRange.Daily => today,
            ReportRange.Weekly => today.AddDays(-6),
            ReportRange.Monthly => today.AddDays(-29),
            ReportRange.HalfYear =>
                new DateTimeOffset(
                    now.Year,
                    now.Month,
                    1,
                    0,
                    0,
                    0,
                    TimeSpan.Zero)
                .AddMonths(-5),
            ReportRange.Yearly =>
                new DateTimeOffset(
                    now.Year,
                    1,
                    1,
                    0,
                    0,
                    0,
                    TimeSpan.Zero),
            ReportRange.All => null,
            _ => today.AddDays(-29)
        };
    }

    private static IReadOnlyList<
        ReportChartPointDto> BuildVisitSeries(
            ReportRange range,
            DateTimeOffset now,
            IReadOnlyCollection<DateTimeOffset> visits)
    {
        return range switch
        {
            ReportRange.Daily => BuildDailySeries(
                now,
                visits),
            ReportRange.Weekly => BuildDaySeries(
                now,
                visits,
                dayCount: 7),
            ReportRange.Monthly => BuildMonthlyDaySeries(
                now,
                visits),
            ReportRange.HalfYear => BuildMonthSeries(
                now,
                visits,
                monthCount: 6),
            ReportRange.Yearly => BuildMonthSeries(
                now,
                visits,
                monthCount: 12),
            ReportRange.All => BuildYearSeries(
                now,
                visits),
            _ => []
        };
    }

    private static IReadOnlyList<
        ReportChartPointDto> BuildDailySeries(
            DateTimeOffset now,
            IReadOnlyCollection<DateTimeOffset> visits)
    {
        var today = now.UtcDateTime.Date;

        return Enumerable.Range(0, 8)
            .Select(
                bucket =>
                {
                    var startHour = bucket * 3;
                    var endHour = startHour + 3;
                    var count = visits.Count(
                        x =>
                            x.UtcDateTime.Date == today &&
                            x.UtcDateTime.Hour >= startHour &&
                            x.UtcDateTime.Hour < endHour);

                    return new ReportChartPointDto(
                        $"{startHour:00}:00",
                        count);
                })
            .ToArray();
    }

    private static IReadOnlyList<
        ReportChartPointDto> BuildDaySeries(
            DateTimeOffset now,
            IReadOnlyCollection<DateTimeOffset> visits,
            int dayCount)
    {
        var endDate = now.UtcDateTime.Date;
        var startDate = endDate.AddDays(-(dayCount - 1));

        return Enumerable.Range(0, dayCount)
            .Select(
                index =>
                {
                    var day = startDate.AddDays(index);
                    var count = visits.Count(
                        x => x.UtcDateTime.Date == day);

                    return new ReportChartPointDto(
                        day.ToString("dd MMM"),
                        count);
                })
            .ToArray();
    }

    private static IReadOnlyList<
        ReportChartPointDto> BuildMonthlyDaySeries(
            DateTimeOffset now,
            IReadOnlyCollection<DateTimeOffset> visits)
    {
        var endDate = now.UtcDateTime.Date.AddDays(1);
        var startDate = endDate.AddDays(-30);

        return Enumerable.Range(0, 10)
            .Select(
                index =>
                {
                    var bucketStart = startDate.AddDays(index * 3);
                    var bucketEnd = bucketStart.AddDays(3);
                    var count = visits.Count(
                        x =>
                            x.UtcDateTime >= bucketStart &&
                            x.UtcDateTime < bucketEnd);

                    return new ReportChartPointDto(
                        bucketStart.ToString("dd MMM"),
                        count);
                })
            .ToArray();
    }

    private static IReadOnlyList<
        ReportChartPointDto> BuildMonthSeries(
            DateTimeOffset now,
            IReadOnlyCollection<DateTimeOffset> visits,
            int monthCount)
    {
        var currentMonth = new DateTime(
            now.Year,
            now.Month,
            1,
            0,
            0,
            0,
            DateTimeKind.Utc);
        var firstMonth = currentMonth
            .AddMonths(-(monthCount - 1));

        return Enumerable.Range(0, monthCount)
            .Select(
                index =>
                {
                    var month = firstMonth.AddMonths(index);
                    var nextMonth = month.AddMonths(1);
                    var count = visits.Count(
                        x =>
                            x.UtcDateTime >= month &&
                            x.UtcDateTime < nextMonth);

                    return new ReportChartPointDto(
                        month.ToString("MMM yy"),
                        count);
                })
            .ToArray();
    }

    private static IReadOnlyList<
        ReportChartPointDto> BuildYearSeries(
            DateTimeOffset now,
            IReadOnlyCollection<DateTimeOffset> visits)
    {
        var firstYear = visits.Count == 0
            ? now.Year
            : visits.Min(x => x.Year);

        return Enumerable.Range(
                firstYear,
                now.Year - firstYear + 1)
            .Select(
                year => new ReportChartPointDto(
                    year.ToString(),
                    visits.Count(x => x.Year == year)))
            .ToArray();
    }

    private static IReadOnlyList<
        ReportDistributionDto> BuildDistribution(
            IEnumerable<MembershipRow> rows)
    {
        var groups = rows
            .GroupBy(x => x.Name)
            .Select(
                group => new
                {
                    Name = group.Key,
                    Count = group
                        .Select(x => x.UserId)
                        .Distinct()
                        .Count()
                })
            .OrderByDescending(x => x.Count)
            .ThenBy(x => x.Name)
            .ToArray();

        var total = groups.Sum(x => x.Count);

        return groups
            .Select(
                group => new ReportDistributionDto(
                    group.Name,
                    group.Count,
                    total == 0
                        ? 0
                        : (int)Math.Round(
                            group.Count * 100m / total)))
            .ToArray();
    }

    private static string GetStatusLabel(
        VerificationStatus status)
    {
        return status switch
        {
            VerificationStatus.Pending => "Bekliyor",
            VerificationStatus.Approved => "Çözüldü",
            VerificationStatus.Rejected => "Reddedildi",
            VerificationStatus.Expired => "Süresi Doldu",
            _ => status.ToString()
        };
    }

    private sealed record MembershipRow(
        Guid UserId,
        string Name);

    private enum ReportRange
    {
        Daily,
        Weekly,
        Monthly,
        HalfYear,
        Yearly,
        All
    }
}
