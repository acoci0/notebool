using Microsoft.EntityFrameworkCore;
using NotMarket.Api.Domain;

namespace NotMarket.Api.Data;

public sealed class AppDbContext(
    DbContextOptions<AppDbContext> options)
    : DbContext(options)
{
    public DbSet<ApplicationUser> Users =>
        Set<ApplicationUser>();

    public DbSet<StudentVerification> StudentVerifications =>
        Set<StudentVerification>();

    public DbSet<AcademicUniversity> AcademicUniversities =>
        Set<AcademicUniversity>();

    public DbSet<AcademicUniversityAlias> AcademicUniversityAliases =>
        Set<AcademicUniversityAlias>();

    public DbSet<AcademicUnit> AcademicUnits =>
        Set<AcademicUnit>();

    public DbSet<AcademicProgram> AcademicPrograms =>
        Set<AcademicProgram>();

    public DbSet<NoteRequest> NoteRequests =>
        Set<NoteRequest>();

    public DbSet<NoteSubmission> NoteSubmissions =>
        Set<NoteSubmission>();

    public DbSet<NoteAiReview> NoteAiReviews =>
    Set<NoteAiReview>();

    public DbSet<AuditLog> AuditLogs =>
        Set<AuditLog>();

    public DbSet<SiteVisit> SiteVisits =>
        Set<SiteVisit>();

    public DbSet<Order> Orders =>
        Set<Order>();

    public DbSet<Payment> Payments =>
        Set<Payment>();

    protected override void OnModelCreating(
        ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigureApplicationUser(modelBuilder);
        ConfigureAcademicUniversity(modelBuilder);
        ConfigureAcademicUniversityAlias(modelBuilder);
        ConfigureAcademicUnit(modelBuilder);
        ConfigureAcademicProgram(modelBuilder);
        ConfigureStudentVerification(modelBuilder);
        ConfigureNoteSubmission(modelBuilder);
        ConfigureNoteAiReview(modelBuilder);
        ConfigureOrder(modelBuilder);
        ConfigurePayment(modelBuilder);
        ConfigureAuditLog(modelBuilder);
        ConfigureSiteVisit(modelBuilder);
    }

    private static void ConfigureApplicationUser(
        ModelBuilder modelBuilder)
    {
        var user =
            modelBuilder.Entity<ApplicationUser>();

        user.HasIndex(x => x.Email)
            .IsUnique();

        user.Property(x => x.Role)
            .HasConversion<string>();

        user.Property(x => x.Status)
            .HasConversion<string>();
    }

    private static void ConfigureAcademicUniversity(
        ModelBuilder modelBuilder)
    {
        var university =
            modelBuilder.Entity<AcademicUniversity>();

        university.ToTable(
            "AcademicUniversities");

        university.HasKey(
            x => x.Id);

        university.Property(
                x => x.CatalogKey)
            .HasMaxLength(100);

        university.HasIndex(
                x => x.CatalogKey)
            .IsUnique();

        university.Property(
                x => x.Name)
            .HasMaxLength(250)
            .IsRequired();

        university.Property(
                x => x.NormalizedName)
            .HasMaxLength(250)
            .IsRequired();

        university.Property(
                x => x.CountryCode)
            .HasMaxLength(2)
            .IsRequired();

        university.Property(
                x => x.City)
            .HasMaxLength(100);

        university.Property(
                x => x.CatalogVersion)
            .HasMaxLength(50);

        university.Property(
                x => x.SourceName)
            .HasMaxLength(200);

        university.HasIndex(
                x => new
                {
                    x.CountryCode,
                    x.NormalizedName
                })
            .IsUnique();

        university.HasIndex(
            x => new
            {
                x.CountryCode,
                x.IsActive
            });
    }

    private static void ConfigureAcademicUniversityAlias(
        ModelBuilder modelBuilder)
    {
        var alias =
            modelBuilder.Entity<AcademicUniversityAlias>();

        alias.ToTable(
            "AcademicUniversityAliases");

        alias.HasKey(
            x => x.Id);

        alias.Property(
                x => x.Alias)
            .HasMaxLength(250)
            .IsRequired();

        alias.Property(
                x => x.NormalizedAlias)
            .HasMaxLength(250)
            .IsRequired();

        alias.HasIndex(
                x => x.NormalizedAlias)
            .IsUnique();

        alias.HasIndex(
            x => x.UniversityId);

        alias.HasOne(
                x => x.University)
            .WithMany(
                x => x.Aliases)
            .HasForeignKey(
                x => x.UniversityId)
            .OnDelete(
                DeleteBehavior.Cascade);
    }

    private static void ConfigureAcademicUnit(
        ModelBuilder modelBuilder)
    {
        var academicUnit =
            modelBuilder.Entity<AcademicUnit>();

        academicUnit.ToTable(
            "AcademicUnits");

        academicUnit.HasKey(
            x => x.Id);

        academicUnit.Property(
                x => x.CatalogKey)
            .HasMaxLength(150);

        academicUnit.HasIndex(
                x => x.CatalogKey)
            .IsUnique();

        academicUnit.Property(
                x => x.Name)
            .HasMaxLength(250)
            .IsRequired();

        academicUnit.Property(
                x => x.NormalizedName)
            .HasMaxLength(250)
            .IsRequired();

        academicUnit.Property(
                x => x.UnitType)
            .HasConversion<string>();

        academicUnit.Property(
                x => x.CatalogVersion)
            .HasMaxLength(50);

        academicUnit.Property(
                x => x.SourceName)
            .HasMaxLength(200);

        academicUnit.HasIndex(
                x => new
                {
                    x.UniversityId,
                    x.NormalizedName
                })
            .IsUnique();

        academicUnit.HasIndex(
            x => new
            {
                x.UniversityId,
                x.IsActive
            });

        academicUnit.HasOne(
                x => x.University)
            .WithMany(
                x => x.AcademicUnits)
            .HasForeignKey(
                x => x.UniversityId)
            .OnDelete(
                DeleteBehavior.Restrict);
    }

    private static void ConfigureAcademicProgram(
        ModelBuilder modelBuilder)
    {
        var academicProgram =
            modelBuilder.Entity<AcademicProgram>();

        academicProgram.ToTable(
            "AcademicPrograms");

        academicProgram.HasKey(
            x => x.Id);

        academicProgram.Property(
                x => x.CatalogKey)
            .HasMaxLength(200);

        academicProgram.HasIndex(
                x => x.CatalogKey)
            .IsUnique();

        academicProgram.Property(
                x => x.Name)
            .HasMaxLength(250)
            .IsRequired();

        academicProgram.Property(
                x => x.NormalizedName)
            .HasMaxLength(250)
            .IsRequired();

        academicProgram.Property(
                x => x.CatalogVersion)
            .HasMaxLength(50);

        academicProgram.Property(
                x => x.SourceName)
            .HasMaxLength(200);

        academicProgram.Property(
                x => x.DegreeLevel)
            .HasMaxLength(50);

        academicProgram.Property(
                x => x.EducationLanguage)
            .HasMaxLength(50);

        academicProgram.Property(
                x => x.IsSelectable)
            .HasDefaultValue(true);

        academicProgram.HasIndex(
                x => new
                {
                    x.AcademicUnitId,
                    x.NormalizedName
                })
            .IsUnique();

        academicProgram.HasIndex(
            x => new
            {
                x.AcademicUnitId,
                x.IsActive
            });

        academicProgram.HasIndex(
            x => new
            {
                x.AcademicUnitId,
                x.IsActive,
                x.IsSelectable
            });

        academicProgram.HasOne(
                x => x.AcademicUnit)
            .WithMany(
                x => x.Programs)
            .HasForeignKey(
                x => x.AcademicUnitId)
            .OnDelete(
                DeleteBehavior.Restrict);
    }

    private static void ConfigureStudentVerification(
        ModelBuilder modelBuilder)
    {
        var verification =
            modelBuilder.Entity<StudentVerification>();

        verification.Property(
                x => x.Status)
            .HasConversion<string>();

        verification.HasIndex(
                x => x.DocumentHash)
            .IsUnique();

        verification.HasOne(
                x => x.University)
            .WithMany(
                x => x.StudentVerifications)
            .HasForeignKey(
                x => x.UniversityId)
            .OnDelete(
                DeleteBehavior.Restrict);

        verification.HasOne(
                x => x.AcademicUnit)
            .WithMany(
                x => x.StudentVerifications)
            .HasForeignKey(
                x => x.AcademicUnitId)
            .OnDelete(
                DeleteBehavior.Restrict);

        verification.HasOne(
                x => x.AcademicProgram)
            .WithMany(
                x => x.StudentVerifications)
            .HasForeignKey(
                x => x.AcademicProgramId)
            .OnDelete(
                DeleteBehavior.Restrict);

        verification.HasIndex(
            x => new
            {
                x.UserId,
                x.UniversityId,
                x.AcademicUnitId,
                x.AcademicProgramId,
                x.Status
            });
    }

    private static void ConfigureNoteSubmission(
        ModelBuilder modelBuilder)
    {
        var noteSubmission =
            modelBuilder.Entity<NoteSubmission>();

        noteSubmission.ToTable(
            "NoteSubmissions",
            table =>
            {
                table.HasCheckConstraint(
                    "CK_NoteSubmissions_SalePrice",
                    "\"SalePrice\" IS NULL OR \"SalePrice\" > 0");

                table.HasCheckConstraint(
                    "CK_NoteSubmissions_PdfGenerationAttemptCount",
                    "\"PdfGenerationAttemptCount\" >= 0");
            });

        noteSubmission.Property(
                x => x.SalePrice)
            .HasPrecision(
                18,
                2);

        noteSubmission.Property(
                x => x.Status)
            .HasConversion<string>();

        noteSubmission.Property(
                x => x.PdfGenerationAttemptCount)
            .HasDefaultValue(
                0);

        noteSubmission.Property(
                x => x.PdfGenerationError)
            .HasMaxLength(
                2000);

        noteSubmission.Property(
                x => x.PdfGenerationModelName)
            .HasMaxLength(
                100);

        noteSubmission.Property(
                x => x.PdfConversionPromptVersion)
            .HasMaxLength(
                100);

        noteSubmission.Property(
                x => x.PdfTemplateVersion)
            .HasMaxLength(
                100);

        noteSubmission.Property(
                x => x.PdfCompilerName)
            .HasMaxLength(
                100);

        noteSubmission.HasIndex(
            x => x.Status);

        noteSubmission.HasIndex(
            x => new
            {
                x.Status,
                x.CreatedAt
            });

        noteSubmission.HasOne(
                x => x.Seller)
            .WithMany(
                x => x.NoteSubmissions)
            .HasForeignKey(
                x => x.SellerId)
            .OnDelete(
                DeleteBehavior.Restrict);
    }
    private static void ConfigureNoteAiReview(
        ModelBuilder modelBuilder)
    {
        var review =
            modelBuilder.Entity<NoteAiReview>();

        review.ToTable(
            "NoteAiReviews",
            table =>
            {
                table.HasCheckConstraint(
                    "CK_NoteAiReviews_ScoresRange",
                    "\"ReadabilityScore\" BETWEEN 0 AND 100 AND " +
                    "\"CourseMatchScore\" BETWEEN 0 AND 100 AND " +
                    "\"DepartmentMatchScore\" BETWEEN 0 AND 100 AND " +
                    "\"ContentCompletenessScore\" BETWEEN 0 AND 100 AND " +
                    "\"OriginalityAndReliabilityScore\" BETWEEN 0 AND 100 AND " +
                    "\"OriginalityRiskScore\" BETWEEN 0 AND 100 AND " +
                    "\"OverallScore\" BETWEEN 0 AND 100 AND " +
                    "\"ConfidenceScore\" BETWEEN 0 AND 100");
            });

        review.HasKey(x => x.Id);

        review.Property(x => x.Decision)
            .HasConversion<string>()
            .HasMaxLength(30);

        review.Property(x => x.Summary)
            .HasMaxLength(2000)
            .IsRequired();

        review.Property(x => x.FindingsJson)
            .HasColumnType("jsonb")
            .IsRequired();

        review.Property(x => x.DetectedCourse)
            .HasMaxLength(220);

        review.Property(x => x.DetectedDepartment)
            .HasMaxLength(220);

        review.Property(x => x.ModelName)
            .HasMaxLength(100)
            .IsRequired();

        review.Property(x => x.PromptVersion)
            .HasMaxLength(50)
            .IsRequired();

        review.HasOne(x => x.NoteSubmission)
            .WithMany(x => x.AiReviews)
            .HasForeignKey(x => x.NoteSubmissionId)
            .OnDelete(DeleteBehavior.Cascade);

        review.HasIndex(x => new
        {
            x.NoteSubmissionId,
            x.ReviewedAt
        });

        review.HasIndex(x => new
        {
            x.Decision,
            x.ReviewedAt
        });
    }
    private static void ConfigureOrder(
        ModelBuilder modelBuilder)
    {
        var order =
            modelBuilder.Entity<Order>();

        order.ToTable(
            "Orders",
            table =>
            {
                table.HasCheckConstraint(
                    "CK_Orders_DifferentUsers",
                    "\"BuyerId\" <> \"SellerId\"");

                table.HasCheckConstraint(
                    "CK_Orders_PositiveAmounts",
                    "\"GrossAmount\" > 0 AND " +
                    "\"PlatformCommissionAmount\" >= 0 AND " +
                    "\"SellerEarningAmount\" >= 0");

                table.HasCheckConstraint(
                    "CK_Orders_AmountBalance",
                    "\"GrossAmount\" = " +
                    "\"PlatformCommissionAmount\" + " +
                    "\"SellerEarningAmount\"");
            });

        order.HasKey(
            x => x.Id);

        order.Property(
                x => x.Status)
            .HasConversion<string>();

        order.Property(
                x => x.GrossAmount)
            .HasPrecision(18, 2);

        order.Property(
                x => x.PlatformCommissionAmount)
            .HasPrecision(18, 2);

        order.Property(
                x => x.SellerEarningAmount)
            .HasPrecision(18, 2);

        order.Property(
                x => x.Currency)
            .HasMaxLength(3)
            .IsRequired();

        order.Property(
                x => x.NoteTitleSnapshot)
            .HasMaxLength(220)
            .IsRequired();

        order.HasOne(
                x => x.Buyer)
            .WithMany()
            .HasForeignKey(
                x => x.BuyerId)
            .OnDelete(
                DeleteBehavior.Restrict);

        order.HasOne(
                x => x.Seller)
            .WithMany()
            .HasForeignKey(
                x => x.SellerId)
            .OnDelete(
                DeleteBehavior.Restrict);

        order.HasOne(
                x => x.NoteSubmission)
            .WithMany(
                x => x.Orders)
            .HasForeignKey(
                x => x.NoteSubmissionId)
            .OnDelete(
                DeleteBehavior.Restrict);

        order.HasIndex(
            x => x.CreatedAt);

        order.HasIndex(
            x => new
            {
                x.BuyerId,
                x.Status
            });

        order.HasIndex(
            x => new
            {
                x.SellerId,
                x.Status
            });

        order.HasIndex(
                x => new
                {
                    x.BuyerId,
                    x.NoteSubmissionId
                })
            .IsUnique()
            .HasFilter(
                "\"Status\" IN ('PendingPayment', 'Paid')");
    }

    private static void ConfigurePayment(
        ModelBuilder modelBuilder)
    {
        var payment =
            modelBuilder.Entity<Payment>();

        payment.ToTable(
            "Payments",
            table =>
            {
                table.HasCheckConstraint(
                    "CK_Payments_PositiveAmount",
                    "\"Amount\" > 0");
            });

        payment.HasKey(
            x => x.Id);

        payment.Property(
                x => x.Status)
            .HasConversion<string>();

        payment.Property(
                x => x.Amount)
            .HasPrecision(18, 2);

        payment.Property(
                x => x.Currency)
            .HasMaxLength(3)
            .IsRequired();

        payment.Property(
                x => x.Provider)
            .HasMaxLength(50)
            .IsRequired();

        payment.Property(
                x => x.ProviderPaymentId)
            .HasMaxLength(200);

        payment.Property(
                x => x.FailureReason)
            .HasMaxLength(600);

        payment.HasOne(
                x => x.Order)
            .WithOne(
                x => x.Payment)
            .HasForeignKey<Payment>(
                x => x.OrderId)
            .OnDelete(
                DeleteBehavior.Cascade);

        payment.HasIndex(
                x => x.OrderId)
            .IsUnique();

        payment.HasIndex(
                x => x.ProviderPaymentId)
            .IsUnique();

        payment.HasIndex(
            x => x.CreatedAt);
    }

    private static void ConfigureAuditLog(
        ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AuditLog>()
            .HasIndex(
                x => x.CreatedAt);
    }

    private static void ConfigureSiteVisit(
        ModelBuilder modelBuilder)
    {
        var siteVisit =
            modelBuilder.Entity<SiteVisit>();

        siteVisit.HasKey(
            x => x.Id);

        siteVisit.HasIndex(
            x => x.VisitedAt);

        siteVisit.HasIndex(
            x => new
            {
                x.SessionHash,
                x.Path,
                x.VisitedAt
            });
    }
}