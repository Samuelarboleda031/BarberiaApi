using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
namespace BarberiaApi.Application.DTOs;

// DTOs para Usuarios
public class UsuarioInput
{
    public string Nombre { get; set; } = string.Empty;
    public string Apellido { get; set; } = string.Empty;
    public string Correo { get; set; } = string.Empty;
    public string? Contrasena { get; set; }
    public int RolId { get; set; }
    public string? TipoDocumento { get; set; }
    public string? Documento { get; set; }
    public string? Telefono { get; set; }
    public string? Direccion { get; set; }
    public string? Barrio { get; set; }
    public DateTime? FechaNacimiento { get; set; }
    public string? FotoPerfil { get; set; }
    public bool Estado { get; set; } = true;
}

public class UsuarioDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Apellido { get; set; } = string.Empty;
    public string Correo { get; set; } = string.Empty;
    public int? RolId { get; set; }
    public string? RolNombre { get; set; }
    public string? TipoDocumento { get; set; }
    public string? Documento { get; set; }
    public string? Telefono { get; set; }
    public string? Direccion { get; set; }
    public string? Barrio { get; set; }
    public DateTime? FechaNacimiento { get; set; }
    public string? FotoPerfil { get; set; }
    public bool Estado { get; set; }
    public DateTime FechaCreacion { get; set; }
    public DateTime? FechaModificacion { get; set; }
    public ClienteDto? Cliente { get; set; }
    public BarberoDto? Barbero { get; set; }
}

// DTOs para Clientes
public class ClienteInput
{
    public int UsuarioId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Apellido { get; set; } = string.Empty;
    public string Documento { get; set; } = string.Empty;
    public string Correo { get; set; } = string.Empty;
    public string? Telefono { get; set; }
    public string? Direccion { get; set; }
    public string? Barrio { get; set; }
    public DateTime? FechaNacimiento { get; set; }
    public string? FotoPerfil { get; set; }
    public bool Estado { get; set; } = true;
    public string Contrasena { get; set; } = string.Empty;
}

public class ClienteDto
{
    public int Id { get; set; }
    public int UsuarioId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Apellido { get; set; } = string.Empty;
    public string Documento { get; set; } = string.Empty;
    public string Correo { get; set; } = string.Empty;
    public string? Telefono { get; set; }
    public string? Direccion { get; set; }
    public string? Barrio { get; set; }
    public DateTime? FechaNacimiento { get; set; }
    public string? FotoPerfil { get; set; }
    public bool Estado { get; set; }
    public DateTime FechaRegistro { get; set; }
    public UsuarioDto? Usuario { get; set; }
}

// DTOs para Barberos
public class BarberoInput
{
    public int UsuarioId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Apellido { get; set; } = string.Empty;
    public string Documento { get; set; } = string.Empty;
    public string Correo { get; set; } = string.Empty;
    public string? Telefono { get; set; }
    public string? Direccion { get; set; }
    public string? Barrio { get; set; }
    public DateTime? FechaNacimiento { get; set; }
    public string Especialidad { get; set; } = "General";
    public string? FotoPerfil { get; set; }
    public bool Estado { get; set; } = true;
    public string Contrasena { get; set; } = string.Empty;
}

public class BarberoDto
{
    public int Id { get; set; }
    public int UsuarioId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Apellido { get; set; } = string.Empty;
    public string Documento { get; set; } = string.Empty;
    public string Correo { get; set; } = string.Empty;
    public string? Telefono { get; set; }
    public string? Direccion { get; set; }
    public string? Barrio { get; set; }
    public DateTime? FechaNacimiento { get; set; }
    public string Especialidad { get; set; } = string.Empty;
    public string? FotoPerfil { get; set; }
    public bool Estado { get; set; }
    public DateTime FechaContratacion { get; set; }
    public UsuarioDto? Usuario { get; set; }
}

// DTOs para Ventas
public class VentaInput
{
    public int UsuarioId { get; set; }
    public int? ClienteId { get; set; }
    public int? BarberoId { get; set; }
    public string? TipoVenta { get; set; }
    public string? MetodoPago { get; set; }
    public decimal? Descuento { get; set; }
    public decimal? IVA { get; set; }
    public decimal? SaldoAFavorUsado { get; set; }
    public bool? UsarSaldoAFavor { get; set; }
    public List<DetalleVentaInput> Detalles { get; set; } = new();
}

public class DetalleVentaInput
{
    public int? ProductoId { get; set; }
    public int? ServicioId { get; set; }
    public int? PaqueteId { get; set; }
    public int Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }
}

// DTOs para Compras
public class CompraInput
{
    public int ProveedorId { get; set; }
    public int UsuarioId { get; set; }
    public string? NumeroFactura { get; set; }
    public DateTime? FechaFactura { get; set; }
    public string? MetodoPago { get; set; }
    public decimal? IVA { get; set; }
    public decimal? Descuento { get; set; }
    public List<DetalleCompraInput> Detalles { get; set; } = new();
}

public class DetalleCompraInput
{
    public int ProductoId { get; set; }
    public int Cantidad { get; set; }
    public int CantidadVentas { get; set; } = 0;
    public int CantidadInsumos { get; set; } = 0;
    public decimal PrecioUnitario { get; set; }
}

// DTOs para Entregas de Insumos
public class EntregaInput
{
    public int BarberoId { get; set; }
    public int UsuarioId { get; set; }
    public List<DetalleEntregaInput> Detalles { get; set; } = new();
    public string? MotivoCategoria { get; set; }
    public string? MotivoDetalle { get; set; }
}

public class DetalleEntregaInput
{
    public int ProductoId { get; set; }
    public int Cantidad { get; set; }
    public decimal? PrecioHistorico { get; set; }
}

public class DetalleEntregaUpdateInput
{
    public int Cantidad { get; set; }
    public decimal? PrecioHistorico { get; set; }
}

public class DetalleEntregaIndividualInput
{
    public int EntregaId { get; set; }
    public int ProductoId { get; set; }
    public int Cantidad { get; set; }
    public decimal? PrecioHistorico { get; set; }
}

// DTOs para Paquetes
public class DetallePaqueteInput
{
    public int ServicioId { get; set; }
    public int Cantidad { get; set; }
}

public class PaqueteInput
{
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public decimal Precio { get; set; }
    public int DuracionMinutos { get; set; }
}

public class PaqueteConDetallesInput
{
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public decimal Precio { get; set; }
    public int DuracionMinutos { get; set; }
    public List<DetallePaqueteInput> Detalles { get; set; } = new();
}

// DTOs para Agendamientos
public class AgendamientoInput
{
    public int ClienteId { get; set; }
    public int BarberoId { get; set; }
    public int? ServicioId { get; set; }
    public List<int>? ServicioIds { get; set; }
    public int? PaqueteId { get; set; }
    public DateTime FechaHora { get; set; }
    public string? Notas { get; set; }
    public string? Duracion { get; set; }
    public decimal? Precio { get; set; }
}

public class AgendamientoDTO
{
    public int Id { get; set; }
    public int ClienteId { get; set; }
    public int BarberoId { get; set; }
    public int? ServicioId { get; set; }
    public List<int> ServicioIds { get; set; } = new();
    public int? PaqueteId { get; set; }
    public string ClienteNombre { get; set; } = string.Empty;
    public string BarberoNombre { get; set; } = string.Empty;
    public string? ServicioNombre { get; set; }
    public List<string> ServiciosNombres { get; set; } = new();
    public string? PaqueteNombre { get; set; }
    public DateTime FechaHora { get; set; }
    public string? Estado { get; set; }
    public string? Duracion { get; set; }
    public decimal? Precio { get; set; }
    public string? Notas { get; set; }
}

// DTOs para Citas (compatibilidad frontend)
public class CitaFrontend
{
    public int id { get; set; }
    public string cliente { get; set; } = string.Empty;
    public string telefono { get; set; } = string.Empty;
    public string servicio { get; set; } = string.Empty;
    public string barbero { get; set; } = string.Empty;
    public string fecha { get; set; } = string.Empty;
    public string hora { get; set; } = string.Empty;
    public int duracion { get; set; }
    public decimal precio { get; set; }
    public string estado { get; set; } = string.Empty;
    public string notas { get; set; } = string.Empty;
}

public class CitaInputFrontend
{
    public string cliente { get; set; } = string.Empty;
    public string telefono { get; set; } = string.Empty;
    public string servicio { get; set; } = string.Empty;
    public string barbero { get; set; } = string.Empty;
    public string fecha { get; set; } = string.Empty;
    public string hora { get; set; } = string.Empty;
    public int duracion { get; set; }
    public decimal precio { get; set; }
    public string estado { get; set; } = string.Empty;
    public string notas { get; set; } = string.Empty;
}

// DTOs Comunes
public class CambioEstadoInput
{
    public string estado { get; set; } = string.Empty;
}

public class CambioEstadoBooleanInput
{
    public bool estado { get; set; }
}

public class CambioEstadoResponse<T>
{
    public T entidad { get; set; } = default!;
    public string mensaje { get; set; } = string.Empty;
    public bool exitoso { get; set; } = false;
}

// DTOs para Proveedores
public class ProveedorNaturalInput
{
    [Required]
    public string Nombre { get; set; } = string.Empty;
    [Required]
    public string NIT { get; set; } = string.Empty;
    public string? Contacto { get; set; }
    [Required]
    public string Correo { get; set; } = string.Empty;
    [Required]
    public string Telefono { get; set; } = string.Empty;
    [Required]
    public string Direccion { get; set; } = string.Empty;
    public string? NumeroIdentificacion { get; set; }
    public string TipoIdentificacion { get; set; } = "CC";
    public string? CorreoContacto { get; set; }
    public string? TelefonoContacto { get; set; }
}

public class ProveedorJuridicoInput
{
    [Required]
    public string Nombre { get; set; } = string.Empty;
    [Required]
    public string NIT { get; set; } = string.Empty;
    public string? Contacto { get; set; }
    [Required]
    public string Correo { get; set; } = string.Empty;
    [Required]
    public string Telefono { get; set; } = string.Empty;
    [Required]
    public string Direccion { get; set; } = string.Empty;
    public string? NumeroIdentificacion { get; set; }
    public string TipoIdentificacion { get; set; } = "NIT";
    public string? CorreoContacto { get; set; }
    public string? TelefonoContacto { get; set; }
}

public class ProveedorUpdateInput
{
    public string Nombre { get; set; } = string.Empty;
    public string? NIT { get; set; }
    public string? Correo { get; set; }
    public string? Telefono { get; set; }
    public string? Direccion { get; set; }
    public bool? Estado { get; set; }
    public string? Contacto { get; set; }
    public string? NumeroIdentificacion { get; set; }
    public string TipoIdentificacion { get; set; } = "CC";
    public string? CorreoContacto { get; set; }
    public string? TelefonoContacto { get; set; }
}

public class ProveedorCreateInput
{
    [Required]
    public string TipoProveedor { get; set; } = string.Empty;
    [Required]
    public string Nombre { get; set; } = string.Empty;
    [Required]
    public string NIT { get; set; } = string.Empty;
    public string? Contacto { get; set; }
    [Required]
    public string Correo { get; set; } = string.Empty;
    [Required]
    public string Telefono { get; set; } = string.Empty;
    [Required]
    public string Direccion { get; set; } = string.Empty;
    public string? NumeroIdentificacion { get; set; }
    public string? TipoIdentificacion { get; set; }
    public string? CorreoContacto { get; set; }
    public string? TelefonoContacto { get; set; }
}

// DTOs para Devoluciones
public class DevolucionInput
{
    public int? VentaId { get; set; }
    public int? ClienteId { get; set; }
    public int UsuarioId { get; set; }
    public int? BarberoId { get; set; }
    public int? EntregaId { get; set; }
    public int ProductoId { get; set; }
    public int Cantidad { get; set; }
    public string? MotivoCategoria { get; set; }
    public string? MotivoDetalle { get; set; }
    public string? Observaciones { get; set; }
    public decimal MontoDevuelto { get; set; }
    public decimal? SaldoAFavor { get; set; }
}

public class DevolucionUpdateInput
{
    public int Id { get; set; }
    public int? VentaId { get; set; }
    public int? ClienteId { get; set; }
    public int UsuarioId { get; set; }
    public int? BarberoId { get; set; }
    public int? EntregaId { get; set; }
    public int ProductoId { get; set; }
    public int Cantidad { get; set; }
    public string? MotivoCategoria { get; set; }
    public string? MotivoDetalle { get; set; }
    public string? Observaciones { get; set; }
    public decimal MontoDevuelto { get; set; }
    public decimal? SaldoAFavor { get; set; }
    public string? Estado { get; set; }
}

public class DevolucionBatchInput
{
    public int VentaId { get; set; }
    public int? ClienteId { get; set; }
    public int UsuarioId { get; set; }
    public int? BarberoId { get; set; }
    public string MotivoCategoria { get; set; } = string.Empty;
    public string? MotivoDetalle { get; set; }
    public string? Observaciones { get; set; }
    public List<DevolucionItem> Items { get; set; } = new();
}

public class DevolucionItem
{
    public int ProductoId { get; set; }
    public int Cantidad { get; set; }
    public decimal MontoDevuelto { get; set; }
}

public class TransferirStockInput
{
    public int Cantidad { get; set; }
    public string Origen { get; set; } = string.Empty;
    public string Destino { get; set; } = string.Empty;
}

// DTOs para Productos
public class ProductoDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public string? Marca { get; set; }
    public decimal PrecioVenta { get; set; }
    public decimal PrecioCompra { get; set; }
    public int StockVentas { get; set; }
    public int StockInsumos { get; set; }
    public int StockTotal { get; set; }
    public int? CategoriaId { get; set; }
    public string? CategoriaNombre { get; set; }
    public bool? Estado { get; set; }
    public string? ImagenProduc { get; set; }
}

// DTOs para Analisis de Usuarios
public class AnalisisUsuarioDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Apellido { get; set; } = string.Empty;
    public string Correo { get; set; } = string.Empty;
    public int? RolId { get; set; }
    public string? RolNombre { get; set; }
    public bool EsCliente { get; set; }
    public bool EsBarbero { get; set; }
    public int VentasHechas { get; set; }
    public int ComprasHechas { get; set; }
    public int DevolucionesProcesadas { get; set; }
    public int EntregasRegistradas { get; set; }
    public int VentasComoCliente { get; set; }
    public int AgendamientosCliente { get; set; }
    public int DevolucionesCliente { get; set; }
    public int AgendamientosBarbero { get; set; }
    public int EntregasBarbero { get; set; }
    public List<string> ModulosAcceso { get; set; } = new();
}

public class CreateUserDto
{
    public string Email { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}

// DTOs para Roles
public class RoleDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public bool Estado { get; set; }
    public int UsuariosAsignados { get; set; }
    public List<int> Modulos { get; set; } = new();
}

public class RoleInput
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public bool Estado { get; set; } = true;
}

// DTOs para Horarios (antes embebidos en HorariosBarberosController)
public class HorarioBarberoCreateInput
{
    public int BarberoId { get; set; }
    public int DiaSemana { get; set; }
    public TimeSpan HoraInicio { get; set; }
    public TimeSpan HoraFin { get; set; }
}

public class HorarioBarberoUpdateInput
{
    public int? BarberoId { get; set; }
    public int? DiaSemana { get; set; }
    public TimeSpan? HoraInicio { get; set; }
    public TimeSpan? HoraFin { get; set; }
    public bool? Estado { get; set; }
}

public class CambioEstadoHorarioInput
{
    public bool estado { get; set; }
    public int UsuarioSolicitanteId { get; set; }
    [JsonPropertyName("fechaHora")]
    public DateTime? FechaHora { get; set; }
    public DateTime? FechaReferencia { get; set; }
    public string? Motivo { get; set; }
    public int CantidadSugerencias { get; set; } = 3;
}

// ServiceResult para respuestas uniformes desde los servicios
public class ServiceResult<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public string? Error { get; set; }
    public int StatusCode { get; set; } = 200;

    public static ServiceResult<T> Ok(T data) => new() { Success = true, Data = data };
    public static ServiceResult<T> Fail(string error, int statusCode = 400)
        => new() { Success = false, Error = error, StatusCode = statusCode };
    public static ServiceResult<T> NotFound(string error = "Recurso no encontrado")
        => new() { Success = false, Error = error, StatusCode = 404 };
}
