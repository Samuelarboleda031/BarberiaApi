using AutoMapper;
using BarberiaApi.Domain.Entities;
using BarberiaApi.Application.DTOs;

namespace BarberiaApi.Application.Mappings;

/// <summary>
/// Perfil centralizado de AutoMapper.
/// Define las reglas de conversión entre Entidades y DTOs.
/// </summary>
public class MappingProfile : Profile
{
    public MappingProfile()
    {
        // ==========================
        // VENTAS
        // ==========================
        CreateMap<Venta, VentaDto>()
            .ForMember(dest => dest.ClienteNombreCompleto, opt => opt.MapFrom(src => 
                src.Cliente != null && src.Cliente.Usuario != null 
                    ? $"{src.Cliente.Usuario.Nombre} {src.Cliente.Usuario.Apellido}" 
                    : (src.ClienteNombre ?? "Cliente")))
            .ForMember(dest => dest.BarberoNombreCompleto, opt => opt.MapFrom(src => 
                src.Barbero != null && src.Barbero.Usuario != null 
                    ? $"{src.Barbero.Usuario.Nombre} {src.Barbero.Usuario.Apellido}" 
                    : "Sin asignar"))
            .ForMember(dest => dest.UsuarioNombreCompleto, opt => opt.MapFrom(src => 
                src.Usuario != null 
                    ? $"{src.Usuario.Nombre} {src.Usuario.Apellido}" 
                    : null))
            .ForMember(dest => dest.Detalles, opt => opt.MapFrom(src => src.DetalleVenta));

        CreateMap<DetalleVenta, DetalleVentaDto>()
            .ForMember(dest => dest.ProductoNombre, opt => opt.MapFrom(src => src.Producto != null ? src.Producto.Nombre : null))
            .ForMember(dest => dest.ServicioNombre, opt => opt.MapFrom(src => src.Servicio != null ? src.Servicio.Nombre : null))
            .ForMember(dest => dest.PaqueteNombre, opt => opt.MapFrom(src => src.Paquete != null ? src.Paquete.Nombre : null))
            .ForMember(dest => dest.FotoUrl, opt => opt.MapFrom(src => src.Producto != null ? src.Producto.ImagenProduc : (src.Servicio != null ? src.Servicio.Imagen : null)));

        CreateMap<DetalleVenta, DetalleVentaInput>().ReverseMap();

        // ==========================
        // COMPRAS
        // ==========================
        CreateMap<Compra, CompraDto>()
            .ForMember(dest => dest.ProveedorNombre, opt => opt.MapFrom(src => src.Proveedor != null ? src.Proveedor.Nombre : null))
            .ForMember(dest => dest.ProveedorNIT, opt => opt.MapFrom(src => src.Proveedor != null ? src.Proveedor.NIT : null))
            .ForMember(dest => dest.UsuarioNombreCompleto, opt => opt.MapFrom(src => 
                src.Usuario != null ? $"{src.Usuario.Nombre} {src.Usuario.Apellido}" : null));

        CreateMap<DetalleCompra, DetalleCompraDto>()
            .ForMember(dest => dest.ProductoNombre, opt => opt.MapFrom(src => src.Producto != null ? src.Producto.Nombre : null));

        CreateMap<DetalleCompra, DetalleCompraInput>().ReverseMap();

        CreateMap<DetalleCompra, DetalleCompraInput>().ReverseMap();

        // ==========================
        // PRODUCTOS
        // ==========================
        CreateMap<Producto, ProductoDto>()
            .ForMember(dest => dest.CategoriaNombre, opt => opt.MapFrom(src => src.Categoria != null ? src.Categoria.Nombre : null));

        CreateMap<Producto, ProductoDto>().ReverseMap();

        // ==========================
        // CLIENTES
        // ==========================
        CreateMap<Cliente, ClienteDto>()
            .ForMember(dest => dest.Nombre, opt => opt.MapFrom(src => src.Usuario.Nombre))
            .ForMember(dest => dest.Apellido, opt => opt.MapFrom(src => src.Usuario.Apellido))
            .ForMember(dest => dest.Correo, opt => opt.MapFrom(src => src.Usuario.Correo));

        CreateMap<ClienteInput, Cliente>();

        // ==========================
        // BARBEROS
        // ==========================
        CreateMap<Barbero, BarberoDto>()
            .ForMember(dest => dest.Nombre, opt => opt.MapFrom(src => src.Usuario.Nombre))
            .ForMember(dest => dest.Apellido, opt => opt.MapFrom(src => src.Usuario.Apellido))
            .ForMember(dest => dest.Correo, opt => opt.MapFrom(src => src.Usuario.Correo));

        CreateMap<BarberoInput, Barbero>();

        // ==========================
        // USUARIOS
        // ==========================
        CreateMap<Usuario, UsuarioDto>()
            .ForMember(dest => dest.RolNombre, opt => opt.MapFrom(src => src.Rol != null ? src.Rol.Nombre : null))
            .ForMember(dest => dest.Telefono, opt => opt.MapFrom(src => src.Cliente != null ? src.Cliente.Telefono : (src.Barbero != null ? src.Barbero.Telefono : null)))
            .ForMember(dest => dest.Direccion, opt => opt.MapFrom(src => src.Cliente != null ? src.Cliente.Direccion : (src.Barbero != null ? src.Barbero.Direccion : null)))
            .ForMember(dest => dest.Barrio, opt => opt.MapFrom(src => src.Cliente != null ? src.Cliente.Barrio : (src.Barbero != null ? src.Barbero.Barrio : null)))
            .ForMember(dest => dest.FechaNacimiento, opt => opt.MapFrom(src => src.Cliente != null ? src.Cliente.FechaNacimiento : (src.Barbero != null ? src.Barbero.FechaNacimiento : null)));

        CreateMap<UsuarioInput, Usuario>();

        // ==========================
        // ROLES Y MODULOS
        // ==========================
        CreateMap<Role, RoleDto>();
        // Modulos y RolesModulos se omiten si no hay DTOs específicos todavía
        
        // ==========================
        // PAQUETES
        // ==========================
        // Omitido temporalmente por falta de DTOs claros

        // ==========================
        // DEVOLUCIONES
        // ==========================
        // Omitido temporalmente por falta de DTOs claros

        // ==========================
        // ENTREGAS INSUMOS
        // ==========================
        // Omitido temporalmente por falta de DTOs claros

        // ==========================
        // AGENDAMIENTOS
        // ==========================
        CreateMap<Agendamiento, AgendamientoDTO>()
            .ForMember(dest => dest.ClienteNombre, opt => opt.MapFrom(src => 
                src.Cliente != null && src.Cliente.Usuario != null 
                    ? $"{src.Cliente.Usuario.Nombre} {src.Cliente.Usuario.Apellido}" 
                    : string.Empty))
            .ForMember(dest => dest.BarberoNombre, opt => opt.MapFrom(src => 
                src.Barbero != null && src.Barbero.Usuario != null 
                    ? $"{src.Barbero.Usuario.Nombre} {src.Barbero.Usuario.Apellido}" 
                    : string.Empty));
    }
}
