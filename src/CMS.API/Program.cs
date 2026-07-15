using CMS.API.Infrastructure;
using CMS.API.Repositories;
using CMS.API.Services;
using Dapper;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

const string CorsPolicy = "LocalhostCors";

// --- Services ---------------------------------------------------------------
builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new() { Title = "CMS API", Version = "v1" });
});

// CORS: allow the Angular dev server (and any localhost origin) during development.
builder.Services.AddCors(options =>
{
    options.AddPolicy(CorsPolicy, policy => policy
        .SetIsOriginAllowed(origin => new Uri(origin).IsLoopback)
        .AllowAnyHeader()
        .AllowAnyMethod());
});

// Data access (Dapper, no EF).
builder.Services.AddSingleton<IDbConnectionFactory, SqlConnectionFactory>();
builder.Services.AddScoped<IAppRoleRepository, AppRoleRepository>();
builder.Services.AddScoped<IAppUserRepository, AppUserRepository>();
builder.Services.AddScoped<IPublishStatusRepository, PublishStatusRepository>();
builder.Services.AddScoped<IPartnerRepository, PartnerRepository>();
builder.Services.AddScoped<ICourseGroupRepository, CourseGroupRepository>();
builder.Services.AddScoped<ICourseRepository, CourseRepository>();
builder.Services.AddScoped<IFeaturedPromoItemRepository, FeaturedPromoItemRepository>();
builder.Services.AddScoped<IRowAuditRepository, RowAuditRepository>();
builder.Services.AddScoped<ILookupRepository, LookupRepository>();
builder.Services.AddScoped<IAuthRepository, AuthRepository>();

// Authentication helpers.
builder.Services.AddSingleton<IJwtTokenService, JwtTokenService>();
builder.Services.AddSingleton<SigningKeyProvider>();

// Cross-cutting row auditing: needs the current request's user, so expose IHttpContextAccessor.
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<RowAuditWriter>();

// JWT bearer authentication. The signing key is resolved at validation time from the
// SysConfig 'appConfig' secret (via SigningKeyProvider) — the same key the AuthController signs with.
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer();

builder.Services
    .AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
    .Configure<SigningKeyProvider>((options, keyProvider) =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            // Tokens carry no issuer/audience (see JwtTokenService); validate signature + lifetime only.
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKeyResolver = (_, _, _, _) => keyProvider.ResolveKeys(),
        };
    });

// Require an authenticated user by default on every endpoint; controllers opt out with [AllowAnonymous].
builder.Services.AddAuthorizationBuilder()
    .SetFallbackPolicy(new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build());

var app = builder.Build();

// Dapper type handlers for DateOnly / TimeOnly columns.
SqlMapper.AddTypeHandler(new DateOnlyTypeHandler());
SqlMapper.AddTypeHandler(new TimeOnlyTypeHandler());

// --- Pipeline ---------------------------------------------------------------
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "CMS API v1");
    options.RoutePrefix = "swagger";
});

app.UseCors(CorsPolicy);
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

// Exposed so the test project's WebApplicationFactory<Program> can boot the app.
public partial class Program;
