using Microsoft.EntityFrameworkCore;
using BarberiaApi.Application.Common;
using BarberiaApi.Application.Interfaces;
using BarberiaApi.Application.Services;
using BarberiaApi.Infrastructure.Data;
using BarberiaApi.Infrastructure.Helpers;
using BarberiaApi.Infrastructure.Services;
using BarberiaApi.Infrastructure.Jobs;
using BarberiaApi.Jobs;

namespace BarberiaApi.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Database
        services.AddDbContextPool<BarberiaContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                b => b.MigrationsAssembly("BarberiaApi"))
        );

        // Cloudinary
        services.Configure<CloudinarySettings>(configuration.GetSection("CloudinarySettings"));
        services.AddScoped<IPhotoService, PhotoService>();

        // Firebase Authentication (borrado de usuarios al eliminarlos del sistema)
        services.AddScoped<IFirebaseAuthService, FirebaseAuthService>();

        // Notificaciones
        services.AddScoped<INotificacionCitasService, NotificacionCitasService>();
        services.AddScoped<IEmailProxyService, EmailProxyService>();
        services.AddScoped<IWhatsAppService, WhatsAppService>();
        services.AddScoped<INotificacionCreditoService, NotificacionCreditoService>();
        services.AddHostedService<NotificacionCreditoJob>();
        services.AddHostedService<RecalcularCreditosJob>();

        return services;
    }

    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // Proveedor de fecha/hora centralizado — elimina la mezcla de DateTime.Now y UtcNow.AddHours(-5)
        services.AddSingleton<IDateTimeProvider, DateTimeProvider>();

        services.AddScoped<IVentaService, VentaService>();
        services.AddScoped<ICompraService, CompraService>();
        services.AddScoped<IAgendamientoService, AgendamientoService>();
        services.AddScoped<ICitaService, CitaService>();
        services.AddScoped<IDevolucionService, DevolucionService>();
        services.AddScoped<IUsuarioService, UsuarioService>();
        services.AddScoped<IProductoService, ProductoService>();
        services.AddScoped<IHorarioService, HorarioService>();
        services.AddScoped<ISolicitudCambioHorarioService, SolicitudCambioHorarioService>();
        services.AddScoped<IClienteService, ClienteService>();
        services.AddScoped<IBarberoService, BarberoService>();
        services.AddScoped<IProveedorService, ProveedorService>();
        services.AddScoped<IPaqueteService, PaqueteService>();
        services.AddScoped<IServicioService, ServicioService>();
        services.AddScoped<ICategoriaService, CategoriaService>();
        services.AddScoped<IRolService, RolService>();
        services.AddScoped<IModuloService, ModuloService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<IImageService, ImageService>();
        services.AddScoped<ICreditoBarberoService, CreditoBarberoService>();
        services.AddScoped<IDescuentoDiaService, DescuentoDiaService>();

        return services;
    }
}
