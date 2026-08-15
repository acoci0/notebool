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
         * Önce üniversiteler oluşturulur.
         */
        await SeedAcademicUniversitiesAsync(
            db,
            cancellationToken);

        /*
         * Akademik fakülte/program yapısı artık
         * versioned AcademicCatalog tarafından
         * yönetilmektedir.
         *
         * Legacy SeedAcademicStructureAsync
         * bilinçli olarak çalıştırılmaz.
         */

        /*
         * Admin kullanıcısı oluşturulur.
         */
        await SeedAdminAsync(
            db,
            configuration,
            cancellationToken);

        /*
         * Sistemde hiç öğrenci yoksa demo
         * kullanıcılar ve demo kayıtlar eklenir.
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

        /*
         * Eski doğrulama kayıtlarını mümkün
         * olduğunda yeni canonical akademik
         * kayıtlara bağlar.
         */
        await BackfillLegacyVerificationsAsync(
            db,
            cancellationToken);
    }

    /*
     * Admin kullanıcısını oluşturur.
     */
    private static async Task SeedAdminAsync(
        AppDbContext db,
        IConfiguration configuration,
        CancellationToken cancellationToken)
    {
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

        if (admin is not null)
        {
            return;
        }

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
     * Türkiye üniversiteleri için başlangıç
     * master datasını oluşturur.
     *
     * İşlem idempotent'tir:
     * uygulama her başladığında aynı kayıtları
     * tekrar eklemez.
     */
    private static async Task SeedAcademicUniversitiesAsync(
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        /*
         * Türkiye üniversiteleri için başlangıç master datası.
         */
        
        var selectedUniversities =
            new[]
            {
                new
                {
                    CatalogKey = "MARMARA",
                    Name = "Marmara Üniversitesi",
                    Aliases = new[]
                    {
                        "Marun",
                        "MARUN"
                    }
                },

                new
                {
                    CatalogKey = "YTU",
                    Name = "Yıldız Teknik Üniversitesi",
                    Aliases = new[]
                    {
                        "YTÜ",
                        "YTU",
                    }
                },

                new
                {
                    CatalogKey = "ITU",
                    Name = "İstanbul Teknik Üniversitesi",
                    Aliases = new[]
                    {
                        "İTÜ",
                        "ITU",
                    }
                },

                new
                {
                    CatalogKey = "HACETTEPE",
                    Name = "Hacettepe Üniversitesi",
                    Aliases = Array.Empty<string>()
                },

                new
                {
                    CatalogKey = "ESTU",
                    Name = "Eskişehir Teknik Üniversitesi",
                    Aliases = new[]
                    {
                        "ESTÜ",
                        "ESTU",
                        
                    }
                },

                new
                {
                    CatalogKey = "BOUN",
                    Name = "Boğaziçi Üniversitesi",
                    Aliases = new[]
                    {
                        "BOUN"
                    }
                },

                new
                {
                    CatalogKey = "METU",
                    Name = "Orta Doğu Teknik Üniversitesi",
                    Aliases = new[]
                    {
                        "ODTÜ",
                        "ODTU",
                        "METU",
                        "Ortadoğu Teknik Üniversitesi",
                        "Ortadoğu Teknik"
                    }
                },

                new
                {
                    CatalogKey = "ANADOLU",
                    Name = "Eskişehir Anadolu Üniversitesi",
                    Aliases = new[]
                    {
                        "Anadolu Üniversitesi"
                    }
                },

                new
                {
                    CatalogKey = "SELCUK",
                    Name = "Selçuk Üniversitesi",
                    Aliases = new[]
                    {
                        "Konya Selçuk Üniversitesi"
                    }
                },

                new
                {
                    CatalogKey = "ISTANBUL",
                    Name = "İstanbul Üniversitesi",
                    Aliases = new[]
                    {
                        "İÜ",
                        "IU"
                    }
                }
            };

        var existingUniversities =
            await db.AcademicUniversities
                .Include(
                    x => x.Aliases)
                .Where(
                    x => x.CountryCode == "TR")
                .ToListAsync(
                    cancellationToken);

        var existingByNormalizedName =
            existingUniversities.ToDictionary(
                x => x.NormalizedName,
                StringComparer.Ordinal);

        var existingAliasesByNormalized =
            existingUniversities
                .SelectMany(
                    x => x.Aliases)
                .ToDictionary(
                    x => x.NormalizedAlias,
                    StringComparer.Ordinal);
        
        var selectedNormalizedNames =
            new HashSet<string>(
                StringComparer.Ordinal);

        foreach (var item in selectedUniversities)
        {
            var normalizedName =
                AcademicTextNormalizer.Normalize(
                    item.Name);

            selectedNormalizedNames.Add(
                normalizedName);

            if (
                existingByNormalizedName.TryGetValue(
                    normalizedName,
                    out var existingUniversity)
            )
            {
                var changed =
                    false;

                if (
                    existingUniversity.Name !=
                    item.Name
                )
                {
                    existingUniversity.Name =
                        item.Name;

                    changed =
                        true;
                }

                if (
                    existingUniversity.NormalizedName !=
                    normalizedName
                )
                {
                    existingUniversity.NormalizedName =
                        normalizedName;

                    changed =
                        true;
                }

                if (
                    existingUniversity.CatalogKey !=
                    item.CatalogKey
                )
                {
                    existingUniversity.CatalogKey =
                        item.CatalogKey;

                    changed =
                        true;
                }

                if (
                    existingUniversity.CountryCode !=
                    "TR"
                )
                {
                    existingUniversity.CountryCode =
                        "TR";

                    changed =
                        true;
                }

                if (!existingUniversity.IsActive)
                {
                    existingUniversity.IsActive =
                        true;

                    changed =
                        true;
                }

                if (changed)
                {
                    existingUniversity.UpdatedAt =
                        DateTimeOffset.UtcNow;
                }

                SyncUniversityAliases(
                    db,
                    existingUniversity,
                    item.Aliases,
                    existingAliasesByNormalized);

                continue;
            }

            var university =
                new AcademicUniversity
                {
                    CatalogKey =
                        item.CatalogKey,

                    Name =
                        item.Name,

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

            SyncUniversityAliases(
                db,
                university,
                item.Aliases,
                existingAliasesByNormalized);
        }

        /*
        * Seçilen 10 üniversitenin dışında kalan
        * Türkiye üniversiteleri silinmez.
        *
        * Eski doğrulama FK'lerini bozmamak için
        * yalnızca pasif hale getirilir.
        */
        foreach (
            var university
            in existingUniversities)
        {
            if (
                selectedNormalizedNames.Contains(
                    university.NormalizedName)
            )
            {
                continue;
            }

            if (!university.IsActive)
            {
                continue;
            }

            university.IsActive =
                false;

            university.UpdatedAt =
                DateTimeOffset.UtcNow;
        }

        await db.SaveChangesAsync(
            cancellationToken);
    }

    private static void SyncUniversityAliases(
        AppDbContext db,
        AcademicUniversity university,
        IEnumerable<string> aliases,
        IDictionary<string, AcademicUniversityAlias> existingAliasesByNormalized)
    {
        var desiredAliases =
            new Dictionary<string, string>(
                StringComparer.Ordinal);

        foreach (var aliasValue in aliases)
        {
            if (string.IsNullOrWhiteSpace(aliasValue))
            {
                continue;
            }

            var alias = aliasValue.Trim();

            var normalizedAlias =
                AcademicTextNormalizer.Normalize(
                    alias);

            if (string.IsNullOrWhiteSpace(normalizedAlias))
            {
                continue;
            }

            /*
             * Canonical üniversite adıyla aynı normalize
             * değere sahip bir alias tutulmaz.
             */
            if (normalizedAlias == university.NormalizedName)
            {
                continue;
            }

            /*
             * Aynı alias farklı yazımlarla tekrar girilmişse
             * ilk tanımı koru. Böylece seed işlemi deterministik
             * ve idempotent kalır.
             */
            desiredAliases.TryAdd(
                normalizedAlias,
                alias);
        }

        /*
         * Artık katalogda bulunmayan eski alias'ları kaldır.
         * Üniversite kaydını silmeden yalnızca alias satırları
         * senkronize edilir.
         */
        var obsoleteAliases =
            university.Aliases
                .Where(
                    x =>
                        !desiredAliases.ContainsKey(
                            x.NormalizedAlias))
                .ToList();

        foreach (var obsoleteAlias in obsoleteAliases)
        {
            db.AcademicUniversityAliases.Remove(
                obsoleteAlias);

            university.Aliases.Remove(
                obsoleteAlias);

            existingAliasesByNormalized.Remove(
                obsoleteAlias.NormalizedAlias);
        }

        foreach (var item in desiredAliases)
        {
            var normalizedAlias = item.Key;
            var alias = item.Value;

            if (
                existingAliasesByNormalized.TryGetValue(
                    normalizedAlias,
                    out var existingAlias)
            )
            {
                /*
                 * Global olarak aynı normalize alias başka bir
                 * üniversiteye bağlıysa sessizce geçmek yerine
                 * seed konfigürasyon hatasını açıkça bildir.
                 */
                if (existingAlias.UniversityId != university.Id)
                {
                    throw new InvalidOperationException(
                        $"Üniversite alias çakışması: '{alias}' alias'ı " +
                        $"birden fazla üniversite için kullanılamaz.");
                }

                if (existingAlias.Alias != alias)
                {
                    existingAlias.Alias = alias;
                }

                continue;
            }

            var universityAlias =
                new AcademicUniversityAlias
                {
                    UniversityId =
                        university.Id,

                    University =
                        university,

                    Alias =
                        alias,

                    NormalizedAlias =
                        normalizedAlias
                };

            db.AcademicUniversityAliases.Add(
                universityAlias);

            university.Aliases.Add(
                universityAlias);

            existingAliasesByNormalized.Add(
                normalizedAlias,
                universityAlias);
        }
    }
    private static async Task SeedAcademicStructureAsync(
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        var structure =
            new[]
            {
                new AcademicUnitSeed(
                    "Marmara Üniversitesi",
                    "Fen Fakültesi",
                    AcademicUnitType.Faculty,
                    new[]
                    {
                        "Matematik",
                        "Fizik",
                        "Kimya",
                        "Biyoloji",
                        "İstatistik"
                    }),

                new AcademicUnitSeed(
                    "Marmara Üniversitesi",
                    "Mühendislik Fakültesi",
                    AcademicUnitType.Faculty,
                    new[]
                    {
                        "Bilgisayar Mühendisliği",
                        "Endüstri Mühendisliği",
                        "Makine Mühendisliği",
                        "Elektrik-Elektronik Mühendisliği"
                    }),

                new AcademicUnitSeed(
                    "Marmara Üniversitesi",
                    "Teknoloji Fakültesi",
                    AcademicUnitType.Faculty,
                    new[]
                    {
                        "Bilgisayar Mühendisliği",
                        "Elektrik-Elektronik Mühendisliği",
                        "Mekatronik Mühendisliği"
                    }),

                new AcademicUnitSeed(
                    "İstanbul Teknik Üniversitesi",
                    "Bilgisayar ve Bilişim Fakültesi",
                    AcademicUnitType.Faculty,
                    new[]
                    {
                        "Bilgisayar Mühendisliği",
                        "Yapay Zeka ve Veri Mühendisliği"
                    }),

                new AcademicUnitSeed(
                    "İstanbul Teknik Üniversitesi",
                    "Elektrik-Elektronik Fakültesi",
                    AcademicUnitType.Faculty,
                    new[]
                    {
                        "Elektrik Mühendisliği",
                        "Elektronik ve Haberleşme Mühendisliği",
                        "Kontrol ve Otomasyon Mühendisliği"
                    }),

                new AcademicUnitSeed(
                    "Boğaziçi Üniversitesi",
                    "Mühendislik Fakültesi",
                    AcademicUnitType.Faculty,
                    new[]
                    {
                        "Bilgisayar Mühendisliği",
                        "Elektrik-Elektronik Mühendisliği",
                        "Endüstri Mühendisliği",
                        "Makine Mühendisliği"
                    }),

                new AcademicUnitSeed(
                    "Boğaziçi Üniversitesi",
                    "Fen-Edebiyat Fakültesi",
                    AcademicUnitType.Faculty,
                    new[]
                    {
                        "Matematik",
                        "Fizik",
                        "Kimya",
                        "Psikoloji"
                    }),

                new AcademicUnitSeed(
                    "Yıldız Teknik Üniversitesi",
                    "Elektrik-Elektronik Fakültesi",
                    AcademicUnitType.Faculty,
                    new[]
                    {
                        "Bilgisayar Mühendisliği",
                        "Elektrik Mühendisliği",
                        "Elektronik ve Haberleşme Mühendisliği"
                    }),

                new AcademicUnitSeed(
                    "Yıldız Teknik Üniversitesi",
                    "Fen-Edebiyat Fakültesi",
                    AcademicUnitType.Faculty,
                    new[]
                    {
                        "Matematik",
                        "Fizik",
                        "Kimya"
                    })
            };

        /*
         * Seed tanımlarında kullanılan
         * üniversiteleri getir.
         */
        var universities =
            await db.AcademicUniversities
                .Where(
                    x =>
                        x.CountryCode == "TR" &&
                        x.IsActive)
                .ToListAsync(
                    cancellationToken);

        var universitiesByNormalizedName =
            universities.ToDictionary(
                x => x.NormalizedName,
                StringComparer.Ordinal);

        /*
         * Yapı tanımlarında kullanılan
         * üniversite ID'lerini belirle.
         */
        var targetUniversityIds =
            new HashSet<Guid>();

        foreach (var item in structure)
        {
            var normalizedUniversityName =
                AcademicTextNormalizer.Normalize(
                    item.UniversityName);

            if (
                !universitiesByNormalizedName.TryGetValue(
                    normalizedUniversityName,
                    out var university)
            )
            {
                throw new InvalidOperationException(
                    $"Akademik yapı seed işlemi için üniversite bulunamadı: {item.UniversityName}");
            }

            targetUniversityIds.Add(
                university.Id);
        }

        /*
         * Mevcut akademik birimleri getir.
         */
        var existingUnits =
            await db.AcademicUnits
                .Where(
                    x =>
                        targetUniversityIds.Contains(
                            x.UniversityId))
                .ToListAsync(
                    cancellationToken);

        var unitsByKey =
            existingUnits.ToDictionary(
                x =>
                    (
                        x.UniversityId,
                        x.NormalizedName
                    ));

        /*
         * Eksik akademik birimleri oluştur.
         */
        foreach (var item in structure)
        {
            var normalizedUniversityName =
                AcademicTextNormalizer.Normalize(
                    item.UniversityName);

            var university =
                universitiesByNormalizedName[
                    normalizedUniversityName];

            var normalizedUnitName =
                AcademicTextNormalizer.Normalize(
                    item.UnitName);

            var key =
                (
                    university.Id,
                    normalizedUnitName
                );

            if (
                unitsByKey.TryGetValue(
                    key,
                    out var existingUnit)
            )
            {
                var changed =
                    false;

                if (
                    existingUnit.Name !=
                    item.UnitName
                )
                {
                    existingUnit.Name =
                        item.UnitName;

                    changed =
                        true;
                }

                if (
                    existingUnit.UnitType !=
                    item.UnitType
                )
                {
                    existingUnit.UnitType =
                        item.UnitType;

                    changed =
                        true;
                }

                if (!existingUnit.IsActive)
                {
                    existingUnit.IsActive =
                        true;

                    changed =
                        true;
                }

                if (changed)
                {
                    existingUnit.UpdatedAt =
                        DateTimeOffset.UtcNow;
                }

                continue;
            }

            var academicUnit =
                new AcademicUnit
                {
                    UniversityId =
                        university.Id,

                    Name =
                        item.UnitName,

                    NormalizedName =
                        normalizedUnitName,

                    UnitType =
                        item.UnitType,

                    IsActive =
                        true
                };

            db.AcademicUnits.Add(
                academicUnit);

            unitsByKey.Add(
                key,
                academicUnit);
        }

        /*
         * Yeni akademik birimlerin ID'leri
         * veritabanına yazılır.
         */
        await db.SaveChangesAsync(
            cancellationToken);

        /*
         * Kullanılan akademik birimlerin
         * programlarını getir.
         */
        var targetUnitIds =
            unitsByKey.Values
                .Select(x => x.Id)
                .ToHashSet();

        var existingPrograms =
            await db.AcademicPrograms
                .Where(
                    x =>
                        targetUnitIds.Contains(
                            x.AcademicUnitId))
                .ToListAsync(
                    cancellationToken);

        var programsByKey =
            existingPrograms.ToDictionary(
                x =>
                    (
                        x.AcademicUnitId,
                        x.NormalizedName
                    ));

        /*
         * Eksik programları oluştur.
         */
        foreach (var item in structure)
        {
            var normalizedUniversityName =
                AcademicTextNormalizer.Normalize(
                    item.UniversityName);

            var university =
                universitiesByNormalizedName[
                    normalizedUniversityName];

            var normalizedUnitName =
                AcademicTextNormalizer.Normalize(
                    item.UnitName);

            var academicUnit =
                unitsByKey[
                    (
                        university.Id,
                        normalizedUnitName
                    )];

            foreach (var programName
                     in item.ProgramNames)
            {
                var normalizedProgramName =
                    AcademicTextNormalizer.Normalize(
                        programName);

                var key =
                    (
                        academicUnit.Id,
                        normalizedProgramName
                    );

                if (
                    programsByKey.TryGetValue(
                        key,
                        out var existingProgram)
                )
                {
                    var changed =
                        false;

                    if (
                        existingProgram.Name !=
                        programName
                    )
                    {
                        existingProgram.Name =
                            programName;

                        changed =
                            true;
                    }

                    if (!existingProgram.IsActive)
                    {
                        existingProgram.IsActive =
                            true;

                        changed =
                            true;
                    }

                    if (changed)
                    {
                        existingProgram.UpdatedAt =
                            DateTimeOffset.UtcNow;
                    }

                    continue;
                }

                var academicProgram =
                    new AcademicProgram
                    {
                        AcademicUnitId =
                            academicUnit.Id,

                        Name =
                            programName,

                        NormalizedName =
                            normalizedProgramName,

                        IsActive =
                            true
                    };

                db.AcademicPrograms.Add(
                    academicProgram);

                programsByKey.Add(
                    key,
                    academicProgram);
            }
        }

        await db.SaveChangesAsync(
            cancellationToken);
    }

    /*
     * Demo öğrenci, doğrulama, talep ve
     * not kaydı oluşturur.
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

        var marmaraUniversity =
            await GetUniversityAsync(
                db,
                "Marmara Üniversitesi",
                cancellationToken);

        var scienceFaculty =
            await GetAcademicUnitAsync(
                db,
                marmaraUniversity.Id,
                "Fen Fakültesi",
                cancellationToken);

        var mathematicsProgram =
            await GetAcademicProgramAsync(
                db,
                scienceFaculty.Id,
                "Matematik",
                cancellationToken);

        var verification =
            new StudentVerification
            {
                User =
                    ayse,

                UniversityId =
                    marmaraUniversity.Id,

                University =
                    marmaraUniversity,

                AcademicUnitId =
                    scienceFaculty.Id,

                AcademicUnit =
                    scienceFaculty,

                AcademicProgramId =
                    mathematicsProgram.Id,

                AcademicProgram =
                    mathematicsProgram,

                UniversityName =
                    marmaraUniversity.Name,

                FacultyName =
                    scienceFaculty.Name,

                DepartmentName =
                    mathematicsProgram.Name,

                DocumentBlobPath =
                    "demo/verifications/ayse.pdf",

                DocumentHash =
                    "demo-hash-ayse",

                DocumentIssueDate =
                    DateOnly.FromDateTime(
                        DateTime.UtcNow.AddDays(-7)),

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
                    mathematicsProgram.Name,

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

    /*
     * Eski doğrulamalarda yalnızca snapshot
     * isimleri varsa canonical ID alanlarını
     * mümkün olduğunda doldurur.
     */
    private static async Task BackfillLegacyVerificationsAsync(
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        var verifications =
            await db.StudentVerifications
                .Where(
                    x =>
                        x.UniversityId == null ||
                        x.AcademicUnitId == null ||
                        x.AcademicProgramId == null)
                .ToListAsync(
                    cancellationToken);

        if (verifications.Count == 0)
        {
            return;
        }

        var universities =
            await db.AcademicUniversities
                .Where(
                    x =>
                        x.CountryCode == "TR" &&
                        x.IsActive)
                .ToListAsync(
                    cancellationToken);

        var universitiesByName =
            universities.ToDictionary(
                x => x.NormalizedName,
                StringComparer.Ordinal);

        var units =
            await db.AcademicUnits
                .Where(x => x.IsActive)
                .ToListAsync(
                    cancellationToken);

        var unitsByKey =
            units.ToDictionary(
                x =>
                    (
                        x.UniversityId,
                        x.NormalizedName
                    ));

        var programs =
            await db.AcademicPrograms
                .Where(x => x.IsActive)
                .ToListAsync(
                    cancellationToken);

        var programsByKey =
            programs.ToDictionary(
                x =>
                    (
                        x.AcademicUnitId,
                        x.NormalizedName
                    ));

        var changed =
            false;

        foreach (var verification in verifications)
        {
            /*
             * Eski kayıtta UniversityId yoksa
             * snapshot üniversite isminden bul.
             */
            if (verification.UniversityId is null)
            {
                var normalizedUniversityName =
                    AcademicTextNormalizer.Normalize(
                        verification.UniversityName);

                if (
                    universitiesByName.TryGetValue(
                        normalizedUniversityName,
                        out var university)
                )
                {
                    verification.UniversityId =
                        university.Id;

                    changed =
                        true;
                }
            }

            if (
                verification.UniversityId is null
            )
            {
                continue;
            }

            /*
             * Akademik birimi FacultyName
             * snapshot alanından eşleştir.
             */
            if (verification.AcademicUnitId is null)
            {
                var normalizedUnitName =
                    AcademicTextNormalizer.Normalize(
                        verification.FacultyName);

                if (
                    unitsByKey.TryGetValue(
                        (
                            verification.UniversityId.Value,
                            normalizedUnitName
                        ),
                        out var academicUnit)
                )
                {
                    verification.AcademicUnitId =
                        academicUnit.Id;

                    changed =
                        true;
                }
            }

            if (
                verification.AcademicUnitId is null
            )
            {
                continue;
            }

            /*
             * Akademik programı DepartmentName
             * snapshot alanından eşleştir.
             */
            if (verification.AcademicProgramId is null)
            {
                var normalizedProgramName =
                    AcademicTextNormalizer.Normalize(
                        verification.DepartmentName);

                if (
                    programsByKey.TryGetValue(
                        (
                            verification.AcademicUnitId.Value,
                            normalizedProgramName
                        ),
                        out var academicProgram)
                )
                {
                    verification.AcademicProgramId =
                        academicProgram.Id;

                    changed =
                        true;
                }
            }
        }

        if (changed)
        {
            await db.SaveChangesAsync(
                cancellationToken);
        }
    }

    /*
     * Canonical üniversite kaydını getirir.
     */
    private static async Task<AcademicUniversity>
        GetUniversityAsync(
            AppDbContext db,
            string universityName,
            CancellationToken cancellationToken)
    {
        var normalizedName =
            AcademicTextNormalizer.Normalize(
                universityName);

        return
            await db.AcademicUniversities
                .SingleOrDefaultAsync(
                    x =>
                        x.CountryCode == "TR" &&
                        x.NormalizedName ==
                            normalizedName,
                    cancellationToken)
            ??
            throw new InvalidOperationException(
                $"Üniversite seed kaydı bulunamadı: {universityName}");
    }

    /*
     * Canonical akademik birim kaydını getirir.
     */
    private static async Task<AcademicUnit>
        GetAcademicUnitAsync(
            AppDbContext db,
            Guid universityId,
            string unitName,
            CancellationToken cancellationToken)
    {
        var normalizedName =
            AcademicTextNormalizer.Normalize(
                unitName);

        return
            await db.AcademicUnits
                .SingleOrDefaultAsync(
                    x =>
                        x.UniversityId ==
                            universityId &&
                        x.NormalizedName ==
                            normalizedName,
                    cancellationToken)
            ??
            throw new InvalidOperationException(
                $"Akademik birim seed kaydı bulunamadı: {unitName}");
    }

    /*
     * Canonical akademik program kaydını getirir.
     */
    private static async Task<AcademicProgram>
        GetAcademicProgramAsync(
            AppDbContext db,
            Guid academicUnitId,
            string programName,
            CancellationToken cancellationToken)
    {
        var normalizedName =
            AcademicTextNormalizer.Normalize(
                programName);

        return
            await db.AcademicPrograms
                .SingleOrDefaultAsync(
                    x =>
                        x.AcademicUnitId ==
                            academicUnitId &&
                        x.NormalizedName ==
                            normalizedName,
                    cancellationToken)
            ??
            throw new InvalidOperationException(
                $"Akademik program seed kaydı bulunamadı: {programName}");
    }

    /*
     * Bir üniversiteye bağlı akademik birim
     * ve program seed tanımı.
     */
    private sealed record AcademicUnitSeed(
        string UniversityName,
        string UnitName,
        AcademicUnitType UnitType,
        IReadOnlyList<string> ProgramNames);
}