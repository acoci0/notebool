using NotMarket.Api.Domain;

namespace NotMarket.Api.Services;

public interface ITokenService
{
    TokenResult CreateAdminToken(ApplicationUser user);
}

public sealed record TokenResult(string AccessToken, DateTimeOffset ExpiresAt);
