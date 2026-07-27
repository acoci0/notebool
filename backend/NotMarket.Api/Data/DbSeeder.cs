using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using NotMarket.Api.Domain;

namespace NotMarket.Api.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(
        AppDbContext db,
        IConfiguration configuration)
    {
        var adminEmail = configuration["SeedAdmin:Email"]
            ?? "admin@notmarket.local";
        var adminPassword = configuration["SeedAdmin:Password"]
            ?? "ChangeMe123!";
        var adminDisplayName = configuration["SeedAdmin:DisplayName"]
            ?? "NotMarket Admin";

        var admin = await db.Users.SingleOrDefaultAsync(
            x => x.Email == adminEmail);

        if (admin is null)
        {
            admin = new ApplicationUser
            {
                Email = adminEmail.Trim().ToLowerInvariant(),
                DisplayName = adminDisplayName,
                PasswordHash = string.Empty,
                Role = UserRole.Admin,
                Status = AccountStatus.Active
            };

            var hasher = new PasswordHasher<ApplicationUser>();
            admin.PasswordHash = hasher.HashPassword(admin, adminPassword);

            db.Users.Add(admin);
            await db.SaveChangesAsync();
        }

        if (!await db.Users.AnyAsync(x => x.Role == UserRole.Student))
        {
            await SeedDemoDataAsync(db);
        }
    }

    private static async Task SeedDemoDataAsync(AppDbContext db)
    {
        var hasher = new PasswordHasher<ApplicationUser>();

        var ayse = new ApplicationUser
        {
            Email = "ayse@example.com",
            DisplayName = "Ayşe Yılmaz",
            PasswordHash = string.Empty,
            Role = UserRole.Student,
            Status = AccountStatus.Active
        };
        ayse.PasswordHash = hasher.HashPassword(ayse, "Student123!");

        var mehmet = new ApplicationUser
        {
            Email = "mehmet@example.com",
            DisplayName = "Mehmet Kaya",
            PasswordHash = string.Empty,
            Role = UserRole.Student,
            Status = AccountStatus.Active
        };
        mehmet.PasswordHash = hasher.HashPassword(mehmet, "Student123!");

        db.Users.AddRange(ayse, mehmet);

        var verification = new StudentVerification
        {
            User = ayse,
            UniversityName = "Marmara Üniversitesi",
            FacultyName = "Fen Fakültesi",
            DepartmentName = "Matematik",
            DocumentBlobPath = "demo/verifications/ayse.pdf",
            DocumentHash = "demo-hash-ayse",
            DocumentIssueDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-7)),
            ExpiresAt = DateTimeOffset.UtcNow.AddMonths(6),
            Status = VerificationStatus.Pending
        };

        var request = new NoteRequest
        {
            BuyerId = ayse.Id,
            UniversityName = "Marmara Üniversitesi",
            DepartmentName = "Matematik",
            CourseName = "Analiz II",
            ClassLevel = 2,
            CriteriaJson = "{\"detailLevel\":\"Detaylı\",\"solvedExamples\":true,\"examType\":\"Final\"}",
            SuggestedMinPrice = 90,
            SuggestedMaxPrice = 140
        };

        var submission = new NoteSubmission
        {
            Request = request,
            Seller = mehmet,
            Title = "Analiz II Final Hazırlık Notu",
            OriginalBlobPath = "demo/notes/analiz-ii-original.pdf",
            GeneratedPdfBlobPath = "demo/notes/analiz-ii-generated.pdf",
            MatchScore = 91,
            ReadabilityScore = 88,
            OriginalityRiskScore = 9,
            Status = NoteSubmissionStatus.ManualReview
        };

        db.StudentVerifications.Add(verification);
        db.NoteRequests.Add(request);
        db.NoteSubmissions.Add(submission);

        await db.SaveChangesAsync();
    }
}
