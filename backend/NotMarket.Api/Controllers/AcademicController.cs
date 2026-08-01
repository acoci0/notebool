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
     * Türkiye'deki aktif üniversiteleri arar.
     *
     * GET:
     * /api/academic/universities?search=mar
     */
    [AllowAnonymous]
    [HttpGet("universities")]
    public async Task<
        ActionResult<IReadOnlyList<AcademicUniversityDto>>>
        SearchUniversities(
            [FromQuery] string? search,
            CancellationToken cancellationToken)
    {
        var normalizedSearch =
            AcademicTextNormalizer.Normalize(
                search);

        /*
         * Çok kısa aramalarda sonuç döndürme.
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
                 * Arama ifadesiyle başlayan
                 * sonuçları önce göster.
                 */
                .OrderBy(
                    x =>
                        x.NormalizedName
                            .StartsWith(
                                normalizedSearch)
                            ? 0
                            : 1)
                .ThenBy(x => x.Name)
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
}