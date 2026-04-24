using Microsoft.EntityFrameworkCore;
using BarberiaApi.Infrastructure.Data;
using BarberiaApi.Infrastructure.Helpers;
using BarberiaApi.Infrastructure.Services;
using BarberiaApi.Domain.Interfaces;
using BarberiaApi.Infrastructure.Repositories;
using BarberiaApi.Application.Interfaces;
using BarberiaApi.Application.Services;

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

        // Unit of Work & Repositories
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Cloudinary
        services.Configure<CloudinarySettings>(configuration.GetSection("CloudinarySettings"));
        services.AddScoped<IPhotoService, PhotoService>();

        // Notificaciones
        services.AddScoped<INotificacionCitasService, NotificacionCitasService>();
        services.AddScoped<IEmailProxyService, EmailProxyService>();

        return services;
    }

    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IVentaService, VentaService>();
        services.AddScoped<ICompraService, CompraService>();
        services.AddScoped<IAgendamientoService, AgendamientoService>();
        services.AddScoped<ICitaService, CitaService>();
        services.AddScoped<IDevolucionService, DevolucionService>();
        services.AddScoped<IEntregaInsumoService, EntregaInsumoService>();
        services.AddScoped<IUsuarioService, UsuarioService>();
        services.AddScoped<IProductoService, ProductoService>();
        services.AddScoped<IHorarioService, HorarioService>();
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

        return services;
    }
}
