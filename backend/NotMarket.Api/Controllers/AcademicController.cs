using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NotMarket.Api.Contracts;
using NotMarket.Api.Data;
using NotMarket.Api.Services;

namespace NotMarket.Api.Controllers;

[ApiController]
[Route("api/academic")]
public sealed class AcademicController(
    AppDbContext db)
    : ControllerBase
{
    /*
     * Türkiye üniversitelerini arar.
     *
     * GET:
     * /api/academic/universities?search=mar
     */
    [AllowAnonymous]
    [HttpGet("universities")]
    public async Task<
        ActionResult<
            IReadOnlyList<AcademicUniversityDto>>>
        SearchUniversities(
            [FromQuery] string? search,
            CancellationToken cancellationToken)
    {
        var normalizedSearch =
            AcademicTextNormalizer.Normalize(
                search);

        /*
         * Çok geniş sorguları engellemek için
         * en az iki karakter zorunludur.
         */
        if (normalizedSearch.Length < 2)
        {
            return Ok(
                Array.Empty<
                    AcademicUniversityDto>());
        }

        var universities =
            await db.AcademicUniversities
                .AsNoTracking()
                .Where(
                    x =>
                        x.IsActive &&
                        x.CountryCode == "TR" &&
                        x.NormalizedName.Contains(
                            normalizedSearch))
                /*
                 * Aranan metinle başlayan sonuçlar
                 * önce gösterilir.
                 */
                .OrderBy(
                    x =>
                        x.NormalizedName.StartsWith(
                            normalizedSearch)
                            ? 0
                            : 1)
                .ThenBy(
                    x => x.Name)
                .Take(10)
                .Select(
                    x =>
                        new AcademicUniversityDto(
                            x.Id,
                            x.Name))
                .ToListAsync(
                    cancellationToken);

        return Ok(universities);
    }

    /*
     * Seçilen üniversiteye bağlı aktif
     * akademik birimleri arar.
     *
     * GET:
     * /api/academic/units
     * ?universityId={guid}
     * &search=fen
     */
    [AllowAnonymous]
    [HttpGet("units")]
    public async Task<
        ActionResult<
            IReadOnlyList<AcademicUnitDto>>>
        SearchAcademicUnits(
            [FromQuery] Guid universityId,
            [FromQuery] string? search,
            CancellationToken cancellationToken)
    {
        if (universityId == Guid.Empty)
        {
            return BadRequest(new
            {
                message =
                    "Geçerli bir üniversite seçmelisiniz."
            });
        }

        var normalizedSearch =
            AcademicTextNormalizer.Normalize(
                search);

        if (normalizedSearch.Length < 2)
        {
            return Ok(
                Array.Empty<
                    AcademicUnitDto>());
        }

        /*
         * Önce üniversitenin aktif ve Türkiye'ye
         * ait olduğunu doğrula.
         */
        var universityExists =
            await db.AcademicUniversities
                .AsNoTracking()
                .AnyAsync(
                    x =>
                        x.Id == universityId &&
                        x.IsActive &&
                        x.CountryCode == "TR",
                    cancellationToken);

        if (!universityExists)
        {
            return NotFound(new
            {
                message =
                    "Üniversite bulunamadı veya aktif değil."
            });
        }

        /*
         * Enum değerini doğrudan SQL içinde
         * ToString yapmamak için önce ara
         * bir nesneye projekte ediyoruz.
         */
        var unitRows =
            await db.AcademicUnits
                .AsNoTracking()
                .Where(
                    x =>
                        x.UniversityId ==
                            universityId &&
                        x.IsActive &&
                        x.NormalizedName.Contains(
                            normalizedSearch))
                .OrderBy(
                    x =>
                        x.NormalizedName.StartsWith(
                            normalizedSearch)
                            ? 0
                            : 1)
                .ThenBy(
                    x => x.Name)
                .Take(15)
                .Select(
                    x => new
                    {
                        x.Id,
                        x.UniversityId,
                        x.Name,
                        x.UnitType
                    })
                .ToListAsync(
                    cancellationToken);

        var units =
            unitRows
                .Select(
                    x =>
                        new AcademicUnitDto(
                            x.Id,
                            x.UniversityId,
                            x.Name,
                            x.UnitType.ToString()))
                .ToList();

        return Ok(units);
    }

    /*
     * Seçilen akademik birime bağlı aktif
     * bölüm veya programları arar.
     *
     * GET:
     * /api/academic/programs
     * ?academicUnitId={guid}
     * &search=mat
     */
    [AllowAnonymous]
    [HttpGet("programs")]
    public async Task<
        ActionResult<
            IReadOnlyList<AcademicProgramDto>>>
        SearchAcademicPrograms(
            [FromQuery] Guid academicUnitId,
            [FromQuery] string? search,
            CancellationToken cancellationToken)
    {
        if (academicUnitId == Guid.Empty)
        {
            return BadRequest(new
            {
                message =
                    "Geçerli bir akademik birim seçmelisiniz."
            });
        }

        var normalizedSearch =
            AcademicTextNormalizer.Normalize(
                search);

        if (normalizedSearch.Length < 2)
        {
            return Ok(
                Array.Empty<
                    AcademicProgramDto>());
        }

        /*
         * Akademik birimin aktif bir Türkiye
         * üniversitesine bağlı olduğunu doğrula.
         */
        var academicUnitExists =
            await db.AcademicUnits
                .AsNoTracking()
                .AnyAsync(
                    x =>
                        x.Id ==
                            academicUnitId &&
                        x.IsActive &&
                        x.University.IsActive &&
                        x.University.CountryCode ==
                            "TR",
                    cancellationToken);

        if (!academicUnitExists)
        {
            return NotFound(new
            {
                message =
                    "Akademik birim bulunamadı veya aktif değil."
            });
        }

        var programs =
            await db.AcademicPrograms
                .AsNoTracking()
                .Where(
                    x =>
                        x.AcademicUnitId ==
                            academicUnitId &&
                        x.IsActive &&
                        x.NormalizedName.Contains(
                            normalizedSearch))
                .OrderBy(
                    x =>
                        x.NormalizedName.StartsWith(
                            normalizedSearch)
                            ? 0
                            : 1)
                .ThenBy(
                    x => x.Name)
                .Take(15)
                .Select(
                    x =>
                        new AcademicProgramDto(
                            x.Id,
                            x.AcademicUnitId,
                            x.Name))
                .ToListAsync(
                    cancellationToken);

        return Ok(programs);
    }
}