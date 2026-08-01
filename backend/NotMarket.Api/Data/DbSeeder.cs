using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using NotMarket.Api.Domain;
using NotMarket.Api.Services;

namespace NotMarket.Api.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(
        AppDbContext db,
        IConfiguration configuration,
        CancellationToken cancellationToken = default)
    {
        /*
         * Üniversite kataloğu, demo öğrenci
         * doğrulamasından önce oluşturulmalıdır.
         */
        await SeedAcademicUniversitiesAsync(
            db,
            cancellationToken);

        /*
         * Admin kullanıcı ayarları.
         */
        var adminEmail =
            (
                configuration["SeedAdmin:Email"] ??
                "admin@notmarket.local"
            )
            .Trim()
            .ToLowerInvariant();

        var adminPassword =
            configuration["SeedAdmin:Password"] ??
            "ChangeMe123!";

        var adminDisplayName =
            configuration["SeedAdmin:DisplayName"] ??
            "NotMarket Admin";

        var admin =
            await db.Users.SingleOrDefaultAsync(
                x => x.Email == adminEmail,
                cancellationToken);

        if (admin is null)
        {
            admin =
                new ApplicationUser
                {
                    Email =
                        adminEmail,

                    DisplayName =
                        adminDisplayName,

                    PasswordHash =
                        string.Empty,

                    Role =
                        UserRole.Admin,

                    Status =
                        AccountStatus.Active
                };

            var hasher =
                new PasswordHasher<ApplicationUser>();

            admin.PasswordHash =
                hasher.HashPassword(
                    admin,
                    adminPassword);

            db.Users.Add(admin);

            await db.SaveChangesAsync(
                cancellationToken);
        }

        /*
         * Sistemde hiç öğrenci yoksa demo
         * kullanıcıları ve demo kayıtları ekle.
         */
        var studentExists =
            await db.Users.AnyAsync(
                x => x.Role == UserRole.Student,
                cancellationToken);

        if (!studentExists)
        {
            await SeedDemoDataAsync(
                db,
                cancellationToken);
        }
    }

    /*
     * Türkiye'deki üniversiteler için başlangıç
     * master datasını oluşturur.
     *
     * Bu işlem idempotent'tir:
     * aynı üniversiteyi tekrar eklemez.
     */
    private static async Task SeedAcademicUniversitiesAsync(
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        /*
         * İlk geliştirme listesi.
         *
         * Bu liste yalnızca Türkiye'deki
         * üniversitelerden oluşmaktadır.
         *
         * İlerleyen aşamada eksiksiz master liste
         * ayrı bir veri dosyasından alınabilir.
         */
        var universityNames =
            new[]
            {
                "Akdeniz Üniversitesi",
                "Anadolu Üniversitesi",
                "Ankara Üniversitesi",
                "Ankara Yıldırım Beyazıt Üniversitesi",
                "Boğaziçi Üniversitesi",
                "Bursa Uludağ Üniversitesi",
                "Çukurova Üniversitesi",
                "Dokuz Eylül Üniversitesi",
                "Ege Üniversitesi",
                "Erciyes Üniversitesi",
                "Eskişehir Osmangazi Üniversitesi",
                "Galatasaray Üniversitesi",
                "Gazi Üniversitesi",
                "Hacettepe Üniversitesi",
                "İstanbul Medeniyet Üniversitesi",
                "İstanbul Teknik Üniversitesi",
                "İstanbul Üniversitesi",
                "İstanbul Üniversitesi-Cerrahpaşa",
                "İzmir Yüksek Teknoloji Enstitüsü",
                "Karadeniz Teknik Üniversitesi",
                "Kocaeli Üniversitesi",
                "Mardin Artuklu Üniversitesi",
                "Marmara Üniversitesi",
                "Ondokuz Mayıs Üniversitesi",
                "Orta Doğu Teknik Üniversitesi",
                "Sağlık Bilimleri Üniversitesi",
                "Sakarya Üniversitesi",
                "Selçuk Üniversitesi",
                "Türk-Alman Üniversitesi",
                "Yıldız Teknik Üniversitesi"
            };

        /*
         * Mevcut Türkiye üniversitelerini getir.
         */
        var existingUniversities =
            await db.AcademicUniversities
                .Where(
                    x => x.CountryCode == "TR")
                .ToListAsync(
                    cancellationToken);

        /*
         * Normalize edilmiş isim üzerinden
         * hızlı mükerrer kontrolü.
         */
        var existingByNormalizedName =
            existingUniversities.ToDictionary(
                x => x.NormalizedName,
                StringComparer.Ordinal);

        foreach (var universityName
                 in universityNames)
        {
            var normalizedName =
                AcademicTextNormalizer.Normalize(
                    universityName);

            if (
                existingByNormalizedName.TryGetValue(
                    normalizedName,
                    out var existingUniversity)
            )
            {
                /*
                 * Kayıt zaten varsa canonical
                 * görünüm değerlerini güncel tut.
                 */
                var changed = false;

                if (
                    existingUniversity.Name !=
                    universityName
                )
                {
                    existingUniversity.Name =
                        universityName;

                    changed = true;
                }

                if (
                    existingUniversity.CountryCode !=
                    "TR"
                )
                {
                    existingUniversity.CountryCode =
                        "TR";

                    changed = true;
                }

                if (!existingUniversity.IsActive)
                {
                    existingUniversity.IsActive =
                        true;

                    changed = true;
                }

                if (changed)
                {
                    existingUniversity.UpdatedAt =
                        DateTimeOffset.UtcNow;
                }

                continue;
            }

            var university =
                new AcademicUniversity
                {
                    Name =
                        universityName,

                    NormalizedName =
                        normalizedName,

                    CountryCode =
                        "TR",

                    IsActive =
                        true
                };

            db.AcademicUniversities.Add(
                university);

            existingByNormalizedName.Add(
                normalizedName,
                university);
        }

        await db.SaveChangesAsync(
            cancellationToken);
    }

    /*
     * Demo öğrenci, doğrulama ve not
     * kayıtlarını oluşturur.
     */
    private static async Task SeedDemoDataAsync(
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        var hasher =
            new PasswordHasher<ApplicationUser>();

        var ayse =
            new ApplicationUser
            {
                Email =
                    "ayse@example.com",

                DisplayName =
                    "Ayşe Yılmaz",

                PasswordHash =
                    string.Empty,

                Role =
                    UserRole.Student,

                Status =
                    AccountStatus.Active
            };

        ayse.PasswordHash =
            hasher.HashPassword(
                ayse,
                "Student123!");

        var mehmet =
            new ApplicationUser
            {
                Email =
                    "mehmet@example.com",

                DisplayName =
                    "Mehmet Kaya",

                PasswordHash =
                    string.Empty,

                Role =
                    UserRole.Student,

                Status =
                    AccountStatus.Active
            };

        mehmet.PasswordHash =
            hasher.HashPassword(
                mehmet,
                "Student123!");

        db.Users.AddRange(
            ayse,
            mehmet);

        /*
         * Demo doğrulaması için canonical
         * Marmara Üniversitesi kaydını getir.
         */
        var marmaraNormalizedName =
            AcademicTextNormalizer.Normalize(
                "Marmara Üniversitesi");

        var marmaraUniversity =
            await db.AcademicUniversities
                .SingleOrDefaultAsync(
                    x =>
                        x.CountryCode == "TR" &&
                        x.NormalizedName ==
                        marmaraNormalizedName,
                    cancellationToken);

        if (marmaraUniversity is null)
        {
            throw new InvalidOperationException(
                "Marmara Üniversitesi seed kaydı bulunamadı.");
        }

        var verification =
            new StudentVerification
            {
                User =
                    ayse,

                UniversityId =
                    marmaraUniversity.Id,

                University =
                    marmaraUniversity,

                UniversityName =
                    marmaraUniversity.Name,

                FacultyName =
                    "Fen Fakültesi",

                DepartmentName =
                    "Matematik",

                DocumentBlobPath =
                    "demo/verifications/ayse.pdf",

                DocumentHash =
                    "demo-hash-ayse",

                DocumentIssueDate =
                    DateOnly.FromDateTime(
                        DateTime.UtcNow.AddDays(-7)),

                /*
                 * Pending kayıt henüz onaylanmadığı
                 * için geçerlilik tarihi yoktur.
                 */
                ExpiresAt =
                    null,

                Status =
                    VerificationStatus.Pending
            };

        var request =
            new NoteRequest
            {
                BuyerId =
                    ayse.Id,

                UniversityName =
                    marmaraUniversity.Name,

                DepartmentName =
                    "Matematik",

                CourseName =
                    "Analiz II",

                ClassLevel =
                    2,

                CriteriaJson =
                    """
                    {
                      "detailLevel": "Detaylı",
                      "solvedExamples": true,
                      "examType": "Final"
                    }
                    """,

                SuggestedMinPrice =
                    90,

                SuggestedMaxPrice =
                    140
            };

        var submission =
            new NoteSubmission
            {
                Request =
                    request,

                Seller =
                    mehmet,

                Title =
                    "Analiz II Final Hazırlık Notu",

                OriginalBlobPath =
                    "demo/notes/analiz-ii-original.pdf",

                GeneratedPdfBlobPath =
                    "demo/notes/analiz-ii-generated.pdf",

                MatchScore =
                    91,

                ReadabilityScore =
                    88,

                OriginalityRiskScore =
                    9,

                Status =
                    NoteSubmissionStatus.ManualReview
            };

        db.StudentVerifications.Add(
            verification);

        db.NoteRequests.Add(
            request);

        db.NoteSubmissions.Add(
            submission);

        await db.SaveChangesAsync(
            cancellationToken);
    }
}