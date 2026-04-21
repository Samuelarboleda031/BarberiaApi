using System.Text.Json.Serialization;
using System.IO.Compression;
using System.Security.Claims;
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

var builder = WebApplication.CreateBuilder(args);

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
                OnTokenValidated = context =>
                {
                    if (context.Principal?.Identity is ClaimsIdentity identity)
                    {
                        var adminClaim = identity.FindFirst("admin")?.Value;
                        var superAdminClaim = identity.FindFirst("super_admin")?.Value;
                        if (string.Equals(adminClaim, "true", StringComparison.OrdinalIgnoreCase)
                            || string.Equals(superAdminClaim, "true", StringComparison.OrdinalIgnoreCase))
                        {
                            identity.AddClaim(new Claim(ClaimTypes.Role, "Admin"));
                        }
                        var explicitRole = identity.FindFirst("role")?.Value ?? identity.FindFirst("rol")?.Value;
                        if (!string.IsNullOrWhiteSpace(explicitRole))
                            identity.AddClaim(new Claim(ClaimTypes.Role, explicitRole));
                    }
                    return Task.CompletedTask;
                }
            };
        });
}

// Authorization Policies
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("ActivePasswordOnly", policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.Requirements.Add(new ValidPasswordRequirement());
    });
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
              .SetVaryByQuery(new[] { "page", "pageSize", "q", "desde", "hasta", "barberoId", "clienteId", "productoId", "entregaId" })
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

app.UseCors("AllowFrontend");
app.UseResponseCompression();
app.UseOutputCache();
app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<ExceptionMiddleware>();
app.MapControllers();

app.Run();
