namespace NotMarket.Api.Contracts;

public sealed record AdminLoginRequest(string Email, string Password);

public sealed record AdminLoginResponse(
    string AccessToken,
    DateTimeOffset ExpiresAt,
    AdminProfileDto Admin);

public sealed record AdminProfileDto(
    Guid Id,
    string Email,
    string DisplayName,
    string Role);

public sealed record StudentLoginRequest(
    string Email,
    string Password);

public sealed record StudentLoginResponse(
    string AccessToken,
    DateTimeOffset ExpiresAt,
    StudentProfileDto Student);

public sealed record StudentProfileDto(
    Guid Id,
    string Email,
    string DisplayName,
    string Role);

