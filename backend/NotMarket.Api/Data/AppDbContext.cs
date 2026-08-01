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

        modelBuilder.Entity<ApplicationUser>()
            .HasIndex(x => x.Email)
            .IsUnique();

        modelBuilder.Entity<ApplicationUser>()
            .Property(x => x.Role)
            .HasConversion<string>();

        modelBuilder.Entity<ApplicationUser>()
            .Property(x => x.Status)
            .HasConversion<string>();


        modelBuilder.Entity<StudentVerification>()
            .Property(x => x.Status)
            .HasConversion<string>();

        modelBuilder.Entity<StudentVerification>()
            .HasIndex(x => x.DocumentHash)
            .IsUnique();


        modelBuilder.Entity<StudentVerification>()
            .HasOne(x => x.University)
            .WithMany(x => x.StudentVerifications)
            .HasForeignKey(x => x.UniversityId)
            .OnDelete(DeleteBehavior.Restrict);


        modelBuilder.Entity<NoteSubmission>()
            .Property(x => x.Status)
            .HasConversion<string>();

        modelBuilder.Entity<NoteSubmission>()
            .HasOne(x => x.Seller)
            .WithMany(x => x.NoteSubmissions)
            .HasForeignKey(x => x.SellerId)
            .OnDelete(DeleteBehavior.Restrict);


        modelBuilder.Entity<AuditLog>()
            .HasIndex(x => x.CreatedAt);

        var academicUniversity =
            modelBuilder.Entity<AcademicUniversity>();

        academicUniversity.ToTable(
            "AcademicUniversities");

        academicUniversity.HasKey(
            x => x.Id);

        academicUniversity
            .Property(x => x.Name)
            .HasMaxLength(250)
            .IsRequired();

        academicUniversity
            .Property(x => x.NormalizedName)
            .HasMaxLength(250)
            .IsRequired();

        academicUniversity
            .Property(x => x.CountryCode)
            .HasMaxLength(2)
            .IsRequired();

        academicUniversity
            .HasIndex(
                x => new
                {
                    x.CountryCode,
                    x.NormalizedName
                })
            .IsUnique();

        academicUniversity
            .HasIndex(x => x.IsActive);
    }
}