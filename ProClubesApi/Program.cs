using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using ProClubesApi.Authorization;
using ProClubesApi.BackgroundJobs;
using ProClubesApi.Data;
using ProClubesApi.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"));
var jwtOptions = builder.Configuration.GetSection("Jwt").Get<JwtOptions>() ?? new JwtOptions();
if (string.IsNullOrWhiteSpace(jwtOptions.SigningKey))
{
    jwtOptions.SigningKey = Environment.GetEnvironmentVariable("PROCLUBES_JWT_SIGNING_KEY") ?? string.Empty;
}
if (string.IsNullOrWhiteSpace(jwtOptions.SigningKey))
{
    throw new InvalidOperationException(
        "A chave 'Jwt:SigningKey' (ou variável de ambiente 'PROCLUBES_JWT_SIGNING_KEY') não foi configurada. Configure-a em appsettings.Development.json ou User Secrets.");
}
builder.Services.PostConfigure<JwtOptions>(opts =>
{
    if (string.IsNullOrWhiteSpace(opts.SigningKey))
        opts.SigningKey = jwtOptions.SigningKey;
});

builder.Services.AddHttpContextAccessor();

// Database configuration: MySQL official provider (with optional SQLite provider for isolated tests/local fallback)
builder.Services.AddDbContext<ProClubesDbContext>((sp, options) =>
{
    var cfg = sp.GetRequiredService<IConfiguration>();
    var dbProvider = cfg["Database:Provider"] ?? "MySql";
    var sqliteConn = cfg.GetConnectionString("SqliteFallback")
                     ?? "Data Source=proclubes_local.db";

    if (string.Equals(dbProvider, "Sqlite", StringComparison.OrdinalIgnoreCase))
    {
        options.UseSqlite(sqliteConn);
    }
    else
    {
        var mysqlConn = cfg.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(mysqlConn))
        {
            mysqlConn = Environment.GetEnvironmentVariable("PROCLUBES_MYSQL_CONNECTION");
        }
        if (string.IsNullOrWhiteSpace(mysqlConn))
        {
            throw new InvalidOperationException(
                "ConnectionString 'DefaultConnection' (ou variável de ambiente 'PROCLUBES_MYSQL_CONNECTION') não foi configurada.");
        }
        options.UseMySQL(mysqlConn);
    }
});

// Services
builder.Services.AddScoped<CurrentUserService>();
builder.Services.AddScoped<AuditoriaService>();
builder.Services.AddScoped<NotificacaoService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<UnidadeService>();
builder.Services.AddScoped<AssociadoAndCadastroService>();
builder.Services.AddScoped<FinanceiroService>();
builder.Services.AddScoped<EventoEspacoReservaService>();
builder.Services.AddScoped<ManifestacaoService>();
builder.Services.AddScoped<VotacaoService>();
builder.Services.AddScoped<DashboardAndSyncService>();
builder.Services.AddScoped<RotinasAutomaticasRunner>();

if (builder.Configuration.GetValue("BackgroundJobs:Enabled", true))
{
    builder.Services.AddHostedService<RotinasBackgroundHostedService>();
}

// Authentication & Authorization
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidAudience = jwtOptions.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SigningKey)),
            ClockSkew = TimeSpan.FromSeconds(15)
        };
    });

builder.Services.AddAuthorization();

// Rate Limiting (API06)
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddFixedWindowLimiter("SensitiveEndpoints", limiterOptions =>
    {
        limiterOptions.PermitLimit = builder.Configuration.GetValue("RateLimiting:PermitLimit", 60);
        limiterOptions.Window = TimeSpan.FromMinutes(1);
        limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        limiterOptions.QueueLimit = 0;
    });
});

// ProblemDetails & Exception Handling (API02)
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ProblemDetailsExceptionHandler>();

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

builder.Services.AddOpenApi();

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

if (app.Configuration.GetValue("Database:AutoInitialize", true))
{
    await DatabaseSeeder.EnsureSeededAsync(app.Services, app.Configuration);
}

app.Run();

public partial class Program { }
