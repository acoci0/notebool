using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NotMarket.Api.Contracts;
using NotMarket.Api.Data;
using NotMarket.Api.Domain;
using NotMarket.Api.Services;

namespace NotMarket.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(
    AppDbContext db,
    ITokenService tokenService) : ControllerBase
{
    [HttpPost("admin/login")]
    public async Task<ActionResult<AdminLoginResponse>> AdminLogin(
        AdminLoginRequest request,
        CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        var user = await db.Users.SingleOrDefaultAsync(
            x => x.Email == email,
            cancellationToken);

        if (user is null ||
            user.Role != UserRole.Admin ||
            user.Status != AccountStatus.Active)
        {
            return Unauthorized(new
            {
                message = "E-posta veya şifre hatalı."
            });
        }

        var hasher = new PasswordHasher<ApplicationUser>();
        var passwordResult = hasher.VerifyHashedPassword(
            user,
            user.PasswordHash,
            request.Password);

        if (passwordResult == PasswordVerificationResult.Failed)
        {
            return Unauthorized(new
            {
                message = "E-posta veya şifre hatalı."
            });
        }

        user.LastLoginAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);

        var token = tokenService.CreateAdminToken(user);

        return Ok(new AdminLoginResponse(
            token.AccessToken,
            token.ExpiresAt,
            new AdminProfileDto(
                user.Id,
                user.Email,
                user.DisplayName,
                user.Role.ToString())));
    }
    [HttpPost("student/login")]
    public async Task<ActionResult<StudentLoginResponse>> StudentLogin(
        StudentLoginRequest request,
        CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        var user = await db.Users.SingleOrDefaultAsync(
            x => x.Email == email,
            cancellationToken);

        if (user is null ||
            user.Role != UserRole.Student ||
            user.Status != AccountStatus.Active)
        {
            return Unauthorized(new
            {
                message = "E-posta veya şifre hatalı."
            });
        }

        var hasher = new PasswordHasher<ApplicationUser>();

        var passwordResult = hasher.VerifyHashedPassword(
            user,
            user.PasswordHash,
            request.Password);

        if (passwordResult == PasswordVerificationResult.Failed)
        {
            return Unauthorized(new
            {
                message = "E-posta veya şifre hatalı."
            });
        }

        user.LastLoginAt = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync(cancellationToken);

        var token = tokenService.CreateAdminToken(user);

        return Ok(new StudentLoginResponse(
            token.AccessToken,
            token.ExpiresAt,
            new StudentProfileDto(
                user.Id,
                user.Email,
                user.DisplayName,
                user.Role.ToString())));
    }
}
