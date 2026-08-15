using System.Net;
using System.Net.Sockets;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using NotMarket.Api.Data;
using NotMarket.Api.Services;
using NotMarket.Api.Data.AcademicCatalog;

var builder = WebApplication.CreateBuilder(args);
builder.Services.Configure<AcademicCatalogImportOptions>(
    builder.Configuration.GetSection(AcademicCatalogImportOptions.SectionName));

builder.Services.AddScoped<AcademicCatalogLoader>();
builder.Services.AddScoped<AcademicCatalogValidator>();
builder.Services.AddScoped<AcademicCatalogImporter>();
builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString(
            "DefaultConnection")));

builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IAuditService, AuditService>();

builder.Services.AddSingleton<
    IVerificationDocumentStorage,
    LocalVerificationDocumentStorage>();

var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException(
        "Jwt:Key tanımlı değil.");

if (Encoding.UTF8.GetByteCount(jwtKey) < 32)
{
    throw new InvalidOperationException(
        "Jwt:Key en az 32 byte uzunluğunda olmalıdır.");
}

builder.Services
    .AddAuthentication(
        JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters =
            new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer =
                    builder.Configuration["Jwt:Issuer"],

                ValidateAudience = true,
                ValidAudience =
                    builder.Configuration["Jwt:Audience"],

                ValidateLifetime = true,

                ValidateIssuerSigningKey = true,
                IssuerSigningKey =
                    new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(jwtKey)),

                ClockSkew = TimeSpan.FromMinutes(1)
            };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(
        "AdminOnly",
        policy => policy.RequireRole("Admin"));

    options.AddPolicy(
        "StudentOnly",
        policy => policy.RequireRole("Student"));
});

/*
 * Development ortamında:
 * - localhost:5173
 * - 127.0.0.1:5173
 * - 192.168.x.x:5173
 * - 10.x.x.x:5173
 * - 172.16.x.x - 172.31.x.x:5173
 *
 * adreslerinden gelen frontend isteklerine izin verir.
 */
builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        if (builder.Environment.IsDevelopment())
        {
            policy
                .SetIsOriginAllowed(
                    IsAllowedDevelopmentOrigin)
                .AllowAnyHeader()
                .AllowAnyMethod();
        }
        else
        {
            var allowedOrigins = builder.Configuration
                .GetSection("Cors:AllowedOrigins")
                .Get<string[]>() ?? [];

            policy
                .WithOrigins(allowedOrigins)
                .AllowAnyHeader()
                .AllowAnyMethod();
        }
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}
else
{
    /*
     * Yerel mobil testte HTTP kullanıyoruz.
     * HTTPS yönlendirmesi yalnızca üretimde devreye girer.
     */
    app.UseHttpsRedirection();
}

app.UseCors("Frontend");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

await using (var scope =
    app.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider
        .GetRequiredService<AppDbContext>();

    await db.Database.MigrateAsync();

    await DbSeeder.SeedAsync(
        db,
        builder.Configuration);

    if (
        app.Configuration.GetValue<bool>(
            "AcademicCatalog:ImportOnStartup")
    )
    {
        var importer =
            scope.ServiceProvider
                .GetRequiredService<
                    AcademicCatalogImporter>();

        var importResult =
            await importer.ImportAsync();

        app.Logger.LogInformation(
            "Academic catalog startup import tamamlandı. " +
            "Version: {CatalogVersion}, " +
            "DryRun: {DryRun}, " +
            "DatabaseModified: {DatabaseModified}, " +
            "UniversitiesAdded: {UniversitiesAdded}, " +
            "UnitsAdded: {UnitsAdded}, " +
            "ProgramsAdded: {ProgramsAdded}",
            importResult.CatalogVersion,
            importResult.DryRun,
            importResult.DatabaseModified,
            importResult.UniversitiesAdded,
            importResult.UnitsAdded,
            importResult.ProgramsAdded);
    }
}

app.Run();

static bool IsAllowedDevelopmentOrigin(
    string origin)
{
    if (!Uri.TryCreate(
            origin,
            UriKind.Absolute,
            out var uri))
    {
        return false;
    }

    var isHttp =
        string.Equals(
            uri.Scheme,
            Uri.UriSchemeHttp,
            StringComparison.OrdinalIgnoreCase);

    var isHttps =
        string.Equals(
            uri.Scheme,
            Uri.UriSchemeHttps,
            StringComparison.OrdinalIgnoreCase);

    if (!isHttp && !isHttps)
    {
        return false;
    }

    /*
     * Vite geliştirme sunucusunun portu.
     */
    if (uri.Port != 5173)
    {
        return false;
    }

    if (string.Equals(
            uri.Host,
            "localhost",
            StringComparison.OrdinalIgnoreCase))
    {
        return true;
    }

    if (uri.Host is "127.0.0.1" or "::1")
    {
        return true;
    }

    if (!IPAddress.TryParse(uri.Host, out var ip))
    {
        return false;
    }

    if (ip.AddressFamily !=
        AddressFamily.InterNetwork)
    {
        return false;
    }

    var bytes = ip.GetAddressBytes();

    /*
     * 10.0.0.0/8
     */
    if (bytes[0] == 10)
    {
        return true;
    }

    /*
     * 172.16.0.0/12
     */
    if (bytes[0] == 172 &&
        bytes[1] >= 16 &&
        bytes[1] <= 31)
    {
        return true;
    }

    /*
     * 192.168.0.0/16
     */
    return bytes[0] == 192 &&
           bytes[1] == 168;
}