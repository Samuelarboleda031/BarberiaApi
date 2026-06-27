using System.Text.Json.Serialization;
using System.IO.Compression;
using System.Security.Claims;
using System.Threading.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using BarberiaApi.Authorization;
using BarberiaApi.Extensions;
using BarberiaApi.Middlewares;
using BarberiaApi.Application.Mappings;
using BarberiaApi.Application.Validators;
using FluentValidation;
using FluentValidation.AspNetCore;
using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// =======================
// Serilog — logging estructurado con traceId correlacionable.
// Lee de la sección "Serilog" en appsettings si existe; si no, usa Console por defecto.
// =======================
builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext()
    .WriteTo.Console());

// =======================
// Firebase Admin SDK
// =======================
try
{
    var serviceAccountPath = builder.Configuration["Firebase:ServiceAccountPath"];
    GoogleCredential credential;
    if (!string.IsNullOrWhiteSpace(serviceAccountPath) && File.Exists(serviceAccountPath))
        credential = GoogleCredential.FromFile(serviceAccountPath);
    else
        credential = GoogleCredential.GetApplicationDefault();

    FirebaseApp.Create(new AppOptions { Credential = credential });
}
catch (Exception ex)
{
    Console.WriteLine("Advertencia: No se pudo inicializar Firebase Admin SDK. " + ex.Message);
}

// =======================
// SERVICES (DI)
// =======================

// Infrastructure: DbContext, Repositories, Cloudinary, Notificaciones
builder.Services.AddInfrastructureServices(builder.Configuration);

// Application: Business logic services
builder.Services.AddApplicationServices();

// AutoMapper & FluentValidation
builder.Services.AddAutoMapper(typeof(MappingProfile).Assembly);
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssembly(typeof(VentaInputValidator).Assembly);

// CORS
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        if (allowedOrigins.Length > 0)
            policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod();
        else
            policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
    });
});

// Authorization Handler
builder.Services.AddSingleton<IAuthorizationHandler, ValidPasswordHandler>();

// Firebase JWT Authentication
var firebaseProjectId = builder.Configuration["Firebase:ProjectId"]
    ?? Environment.GetEnvironmentVariable("FIREBASE_PROJECT_ID");

if (!string.IsNullOrWhiteSpace(firebaseProjectId))
{
    builder.Services
        .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.Authority = $"https://securetoken.google.com/{firebaseProjectId}";
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = $"https://securetoken.google.com/{firebaseProjectId}",
                ValidateAudience = true,
                ValidAudience = firebaseProjectId,
                ValidateLifetime = true,
                NameClaimType = "name",
                RoleClaimType = ClaimTypes.Role
            };
    options.Events = new JwtBearerEvents
            {
                OnTokenValidated = async context =>
                {
                    if (context.Principal?.Identity is not ClaimsIdentity identity) return;

                    // 1. Intentar rol desde custom claims de Firebase
                    var adminClaim = identity.FindFirst("admin")?.Value;
                    var superAdminClaim = identity.FindFirst("super_admin")?.Value;
                    if (string.Equals(superAdminClaim, "true", StringComparison.OrdinalIgnoreCase))
                    {
                        identity.AddClaim(new Claim(ClaimTypes.Role, "super_admin"));
                        identity.AddClaim(new Claim(ClaimTypes.Role, "Admin"));
                        identity.AddClaim(new Claim(ClaimTypes.Role, "admin"));
                    }
                    else if (string.Equals(adminClaim, "true", StringComparison.OrdinalIgnoreCase))
                    {
                        identity.AddClaim(new Claim(ClaimTypes.Role, "Admin"));
                        identity.AddClaim(new Claim(ClaimTypes.Role, "admin"));
                    }

                    var explicitRole = identity.FindFirst("role")?.Value ?? identity.FindFirst("rol")?.Value;
                    if (!string.IsNullOrWhiteSpace(explicitRole))
                        identity.AddClaim(new Claim(ClaimTypes.Role, explicitRole));

                    // 2. Si aún no tiene rol, buscar en la BD por email
                    if (!identity.HasClaim(c => c.Type == ClaimTypes.Role))
                    {
                        var email = identity.FindFirst("email")?.Value
                                    ?? identity.FindFirst(ClaimTypes.Email)?.Value;

                        // Log de diagnóstico — remover en producción
                        var logger = context.HttpContext.RequestServices
                            .GetRequiredService<ILoggerFactory>()
                            .CreateLogger("TokenValidated");
                        logger.LogInformation("OnTokenValidated — email: {Email}, claims: {Claims}",
                            email ?? "(null)",
                            string.Join(", ", identity.Claims.Select(c => $"{c.Type}={c.Value}")));

                        if (!string.IsNullOrWhiteSpace(email))
                        {
                            var roleAssigned = false;
                            try
                            {
                                var db = context.HttpContext.RequestServices
                                    .GetRequiredService<BarberiaApi.Infrastructure.Data.BarberiaContext>();

                                var usuario = await db.Usuarios
                                    .AsNoTracking()
                                    .Include(u => u.Rol)
                                    .FirstOrDefaultAsync(u => u.Correo == email);

                                logger.LogInformation("BD lookup — usuario: {Usuario}, rol: {Rol}",
                                    usuario?.Correo ?? "(no encontrado)",
                                    usuario?.Rol?.Nombre ?? "(sin rol)");

                                if (usuario != null)
                                {
                                    // Add NameIdentifier claim with the usuario's Id
                                    identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, usuario.Id.ToString()));

                                    if (usuario.Rol != null)
                                    {
                                        var nombreRol = usuario.Rol.Nombre?.ToLower().Trim() ?? "";
                                        // Usar Contains para cubrir variantes: "Super Administrador", "super_admin", etc.
                                        if (nombreRol.Contains("super") && (nombreRol.Contains("admin") || nombreRol.Contains("administrador")))
                                        {
                                            identity.AddClaim(new Claim(ClaimTypes.Role, "super_admin"));
                                            identity.AddClaim(new Claim(ClaimTypes.Role, "Admin"));
                                            identity.AddClaim(new Claim(ClaimTypes.Role, "admin"));
                                        }
                                        else if (nombreRol.Contains("admin") || nombreRol.Contains("administrador") || nombreRol.Contains("gerente"))
                                        {
                                            identity.AddClaim(new Claim(ClaimTypes.Role, "Admin"));
                                            identity.AddClaim(new Claim(ClaimTypes.Role, "admin"));
                                        }
                                        else if (nombreRol.Contains("barbero"))
                                        {
                                            identity.AddClaim(new Claim(ClaimTypes.Role, "barbero"));
                                        }
                                        else
                                        {
                                            identity.AddClaim(new Claim(ClaimTypes.Role, "cliente"));
                                        }

                                        if (usuario.RolId.HasValue)
                                            identity.AddClaim(new Claim("rolId", usuario.RolId.Value.ToString()));

                                        roleAssigned = true;
                                    }
                                }
                            }
                            catch
                            {
                                // BD no disponible — continuar con fallback
                            }

                            // Fallback: si la BD no está disponible, usar el email del admin configurado
                            if (!roleAssigned)
                            {
                                var adminEmail = builder.Configuration["AdminEmail"]
                                    ?? Environment.GetEnvironmentVariable("ADMIN_EMAIL");
                                if (!string.IsNullOrWhiteSpace(adminEmail)
                                    && string.Equals(email, adminEmail, StringComparison.OrdinalIgnoreCase))
                                {
                                    identity.AddClaim(new Claim(ClaimTypes.Role, "Admin"));
                                    identity.AddClaim(new Claim(ClaimTypes.Role, "admin"));
                                }
                            }
                        }
                    }
                }
            };
        });
}

// Authorization Policies — FallbackPolicy exige auth en todo endpoint no marcado con [AllowAnonymous]
builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();

    options.AddPolicy("ActivePasswordOnly", policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.Requirements.Add(new ValidPasswordRequirement());
    });
});

// FE-A3 — HttpClient para verificar el token de captcha contra Cloudflare Turnstile.
builder.Services.AddHttpClient();

// FE-A2 / Fase 4 — Rate limiting. La política "auth" protege los endpoints sensibles
// de autenticación/gestión de usuarios contra abuso y fuerza bruta. El particionado
// es por IP del cliente. Devuelve 429 al superar el límite.
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("auth", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));
});

// Controllers + JSON config
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    })
    .ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory = context =>
        {
            var navigationProperties = new[]
            {
                "Barbero", "Cliente", "Servicio", "Paquete", "Proveedor",
                "Producto", "Venta", "Compra", "DetalleVenta", "DetalleCompras",
                "Categoria", "Rol", "Empleado", "UsuarioRegistra", "Modulo"
            };
            foreach (var prop in navigationProperties)
            {
                if (context.ModelState.ContainsKey(prop))
                    context.ModelState.Remove(prop);
            }
            if (context.ModelState.IsValid)
                return new Microsoft.AspNetCore.Mvc.BadRequestObjectResult(context.ModelState);
            var result = new Microsoft.AspNetCore.Mvc.BadRequestObjectResult(context.ModelState);
            result.ContentTypes.Add("application/json");
            return result;
        };
    });

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Form Options
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 15728640;
});

// Response Compression
builder.Services.AddResponseCompression(opt =>
{
    opt.EnableForHttps = true;
    opt.Providers.Add<GzipCompressionProvider>();
    opt.Providers.Add<BrotliCompressionProvider>();
    opt.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(new[] { "application/json" });
});
builder.Services.Configure<GzipCompressionProviderOptions>(o => o.Level = CompressionLevel.Fastest);
builder.Services.Configure<BrotliCompressionProviderOptions>(o => o.Level = CompressionLevel.Fastest);

// Output Cache
builder.Services.AddOutputCache(options =>
{
    options.AddPolicy("short", policy =>
        policy.Expire(TimeSpan.FromSeconds(30))
              .SetVaryByQuery(new[] { "page", "pageSize", "q", "desde", "hasta", "barberoId", "clienteId", "productoId", "entregaId", "estaSemana" })
              .SetVaryByRouteValue("id")
              .SetVaryByRouteValue("barberoId")
              .SetVaryByRouteValue("clienteId")
              .SetVaryByRouteValue("paqueteId")
              .SetVaryByRouteValue("fecha")
    );
});

var app = builder.Build();

// =======================
// MIDDLEWARE PIPELINE
// =======================

// Logging estructurado de cada request (método, ruta, status, duración, traceId).
app.UseSerilogRequestLogging();

if (app.Environment.IsDevelopment() || builder.Configuration.GetValue<bool>("Swagger:Enable"))
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Barberia API v1");
        c.RoutePrefix = app.Environment.IsDevelopment() ? string.Empty : "swagger";
    });
}

var httpsRedirect = builder.Configuration.GetValue<bool>("Https:Redirect", true);
if (httpsRedirect)
    app.UseHttpsRedirection();

app.UseMiddleware<ExceptionMiddleware>();
app.UseCors("AllowFrontend");
app.UseResponseCompression();
app.UseOutputCache();
app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();
app.MapControllers();

app.MapMethods("/health", new[] { "GET", "HEAD" }, async (BarberiaApi.Infrastructure.Data.BarberiaContext db) =>
{
    try
    {
        await db.Database.CanConnectAsync();
        return Results.Ok(new { status = "ok", database = "connected", timestamp = DateTime.UtcNow });
    }
    catch (Exception ex)
    {
        return Results.Ok(new { status = "warning", database = "disconnected", error = ex.Message, timestamp = DateTime.UtcNow });
    }
}).AllowAnonymous();

app.Run();
