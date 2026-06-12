using System.Text.Json.Serialization;
using LicitApp.Api.Auth;
using LicitApp.Api.Data;
using LicitApp.Api.Json;
using LicitApp.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.DataProtection;

var builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------------------------
// JSON: camelCase + enums como string + DateTime siempre UTC.
// ---------------------------------------------------------------------------
builder.Services
    .AddControllers()
    .AddJsonOptions(o =>
    {
        o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        o.JsonSerializerOptions.Converters.Add(new UtcDateTimeConverter());
        o.JsonSerializerOptions.Converters.Add(new NullableUtcDateTimeConverter());
    });

// ---------------------------------------------------------------------------
// EF Core + PostgreSQL (Npgsql).
// ---------------------------------------------------------------------------
var connectionString = builder.Configuration.GetConnectionString("Default")
    ?? "Host=localhost;Port=5432;Database=licitapp;Username=licitapp;Password=licitapp_dev";

builder.Services.AddDbContext<AppDbContext>(opt => opt.UseNpgsql(connectionString));

// ---------------------------------------------------------------------------
// Auth: verificación de Firebase ID Tokens (JWT firmados por Google).
// No requiere Admin SDK: JwtBearer descubre el JWKS público vía Authority.
// ---------------------------------------------------------------------------
var projectId = builder.Configuration["Firebase:ProjectId"] ?? "licitapp-e1841";
var issuer = $"https://securetoken.google.com/{projectId}";

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = issuer;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = issuer,
            ValidateAudience = true,
            ValidAudience = projectId,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
        };
    });

builder.Services.AddAuthorization();

// ---------------------------------------------------------------------------
// CORS: permisivo en desarrollo, restringido por origen en producción.
// ---------------------------------------------------------------------------
const string CorsPolicy = "licitapp";
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? Array.Empty<string>();

builder.Services.AddCors(o => o.AddPolicy(CorsPolicy, p =>
{
    if (builder.Environment.IsDevelopment() || allowedOrigins.Length == 0)
        p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
    else
        p.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod();
}));

// ---------------------------------------------------------------------------
// Swagger / OpenAPI con esquema Bearer.
// ---------------------------------------------------------------------------
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "LicitApp API", Version = "v1" });
    var scheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Firebase ID Token. Formato: Bearer {token}",
        Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" },
    };
    c.AddSecurityDefinition("Bearer", scheme);
    c.AddSecurityRequirement(new OpenApiSecurityRequirement { [scheme] = Array.Empty<string>() });
});

// ---------------------------------------------------------------------------
// Servicios de negocio.
// ---------------------------------------------------------------------------
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<ISolicitudService, SolicitudService>();
builder.Services.AddScoped<IOfertaService, OfertaService>();
builder.Services.AddScoped<INotificationService, NotificationService>();

// Job en background: notificaciones DEADLINE_NEAR.
builder.Services.Configure<DeadlineNearOptions>(builder.Configuration.GetSection("Notifications:DeadlineNear"));
builder.Services.AddHostedService<DeadlineNearService>();

builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo("/app/keys"));

var app = builder.Build();

// Aplicar migraciones al arranque (crea/actualiza el esquema).
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

app.UseMiddleware<ExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "LicitApp API v1"));
}

app.UseCors(CorsPolicy);
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Health-check (liveness) sin auth.
app.MapGet("/health", () => Results.Ok(new { status = "ok" })).AllowAnonymous();

app.Run();
