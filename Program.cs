using Microsoft.EntityFrameworkCore;
using BarberiaApi.Models;
using System.Text.Json.Serialization;
using BarberiaApi.Helpers;
using BarberiaApi.Services;
using BarberiaApi.Authorization;
using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Microsoft.AspNetCore.Authorization;
using System.IO;
using Microsoft.AspNetCore.Http.Features;

var builder = WebApplication.CreateBuilder(args);

try
{
    var serviceAccountPath = builder.Configuration["Firebase:ServiceAccountPath"];
    GoogleCredential credential;
    if (!string.IsNullOrWhiteSpace(serviceAccountPath) && File.Exists(serviceAccountPath))
    {
        credential = GoogleCredential.FromFile(serviceAccountPath);
    }
    else
    {
        credential = GoogleCredential.GetApplicationDefault();
    }

    FirebaseApp.Create(new AppOptions { Credential = credential });
}
catch (Exception ex)
{
    Console.WriteLine("Advertencia: No se pudo inicializar Firebase Admin SDK. " + ex.Message);
}

// =======================
// 🔧 SERVICES
// =======================

// 📦 Base de datos
builder.Services.AddDbContext<BarberiaContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"))
);

// 🌐 CORS (permitir frontend)
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        if (allowedOrigins.Length > 0)
        {
            policy.WithOrigins(allowedOrigins)
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        }
        else
        {
            policy
                .AllowAnyOrigin()
                .AllowAnyHeader()
                .AllowAnyMethod();
        }
    });
});

// Registrar el Handler en el contenedor de dependencias
builder.Services.AddSingleton<IAuthorizationHandler, ValidPasswordHandler>();

// Configurar la Política de Autorización
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("ActivePasswordOnly", policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.Requirements.Add(new ValidPasswordRequirement());
    });
});

// 🎮 Controllers + JSON config
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
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

// 📘 Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ✨ Cloudinary Configuration
builder.Services.Configure<CloudinarySettings>(builder.Configuration.GetSection("CloudinarySettings"));
builder.Services.AddScoped<IPhotoService, PhotoService>();

builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 15728640;
});

// ✉️ Email & Password Reset deshabilitado

var app = builder.Build();

// =======================
// 🚀 MIDDLEWARE
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
{
    app.UseHttpsRedirection();
}

// 🌐 CORS (IMPORTANTE: antes de Authorization)
app.UseCors("AllowFrontend");

app.UseStaticFiles();

app.UseAuthorization();

app.MapControllers();

app.Run();
