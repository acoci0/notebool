using Microsoft.EntityFrameworkCore;
using NotMarket.Api.Domain;

namespace NotMarket.Api.Data;

public sealed class AppDbContext(
    DbContextOptions<AppDbContext> options)
    : DbContext(options)
{
    public DbSet<ApplicationUser> Users =>
        Set<ApplicationUser>();

    public DbSet<StudentVerification>
        StudentVerifications =>
            Set<StudentVerification>();

    public DbSet<AcademicUniversity>
        AcademicUniversities =>
            Set<AcademicUniversity>();

    public DbSet<AcademicUniversityAlias>
        AcademicUniversityAliases =>
            Set<AcademicUniversityAlias>();

    public DbSet<AcademicUnit> AcademicUnits =>
        Set<AcademicUnit>();

    public DbSet<AcademicProgram> AcademicPrograms =>
        Set<AcademicProgram>();

    public DbSet<NoteRequest> NoteRequests =>
        Set<NoteRequest>();

    public DbSet<NoteSubmission> NoteSubmissions =>
        Set<NoteSubmission>();

    public DbSet<AuditLog> AuditLogs =>
        Set<AuditLog>();

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
        ConfigureAuditLog(modelBuilder);
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

        /*
         * Katalog kapsamındaki üniversiteler
         * kalıcı CatalogKey üzerinden eşleştirilir.
         *
         * Alan nullable olduğu için eski seed
         * kayıtlarının migration sırasında
         * bozulmasına neden olmaz.
         */
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

        /*
         * Aynı ülkede aynı normalize edilmiş
         * üniversite adı yalnızca bir kez bulunabilir.
         */
        university.HasIndex(
                x => new
                {
                    x.CountryCode,
                    x.NormalizedName
                })
            .IsUnique();

        /*
         * Aktif üniversite aramalarını hızlandırır.
         */
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

        /*
         * Aynı normalize edilmiş alias iki farklı
         * üniversiteye bağlanamaz.
         *
         * Örnek:
         * "odtu" yalnızca Orta Doğu Teknik
         * Üniversitesi kaydına ait olabilir.
         */
        alias.HasIndex(
                x => x.NormalizedAlias)
            .IsUnique();

        /*
         * Bir üniversiteye bağlı alias kayıtlarının
         * sorgulanmasını hızlandırır.
         */
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

        /*
         * Fakülte veya diğer akademik birimler
         * katalog anahtarına göre eşleştirilir.
         */
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

        /*
         * Aynı üniversitede aynı normalize edilmiş
         * akademik birim adı tekrar oluşturulamaz.
         *
         * Örnek:
         * Marmara Üniversitesi içerisinde
         * "Fen Fakültesi" yalnızca bir kez bulunur.
         */
        academicUnit.HasIndex(
                x => new
                {
                    x.UniversityId,
                    x.NormalizedName
                })
            .IsUnique();

        /*
         * Üniversiteye bağlı aktif akademik
         * birim sorgularını hızlandırır.
         */
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

        /*
         * Bölüm veya programlar kalıcı katalog
         * anahtarı üzerinden eşleştirilir.
         */
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

        /*
         * Yeni ve mevcut programların varsayılan
         * olarak öğrenci formunda seçilebilir
         * olmasını sağlar.
         */
        academicProgram.Property(
                x => x.IsSelectable)
            .HasDefaultValue(true);

        /*
         * Aynı akademik birim altında aynı
         * normalize edilmiş bölüm veya program
         * adı tekrar oluşturulamaz.
         */
        academicProgram.HasIndex(
                x => new
                {
                    x.AcademicUnitId,
                    x.NormalizedName
                })
            .IsUnique();

        /*
         * Akademik birime bağlı aktif program
         * sorgularını hızlandırır.
         */
        academicProgram.HasIndex(
            x => new
            {
                x.AcademicUnitId,
                x.IsActive
            });

        /*
         * Öğrenci formunda gösterilecek programların
         * sorgulanmasını hızlandırır.
         */
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

        /*
         * Aynı PDF dosyasının birden fazla
         * doğrulamada kullanılmasını engeller.
         */
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

        /*
         * Aynı öğrencinin aynı akademik alan
         * için yaptığı doğrulama kontrollerini
         * hızlandırır.
         *
         * Bu indeks unique değildir. Geçmişte
         * reddedilen veya süresi dolan kayıtların
         * saklanmasına izin verir.
         */
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

        noteSubmission.Property(
                x => x.Status)
            .HasConversion<string>();

        noteSubmission.HasOne(
                x => x.Seller)
            .WithMany(
                x => x.NoteSubmissions)
            .HasForeignKey(
                x => x.SellerId)
            .OnDelete(
                DeleteBehavior.Restrict);
    }

    private static void ConfigureAuditLog(
        ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AuditLog>()
            .HasIndex(
                x => x.CreatedAt);
    }
}