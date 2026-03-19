using BarberiaApi.Application.DTOs;
using BarberiaApi.Application.Interfaces;
using BarberiaApi.Domain.Entities;
using BarberiaApi.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BarberiaApi.Application.Services;

public class ClienteService : IClienteService
{
    private readonly BarberiaContext _context;

    public ClienteService(BarberiaContext context) => _context = context;

    public async Task<ServiceResult<object>> GetAllAsync(int page, int pageSize, string? q)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 5;
        var baseQ = _context.Clientes.Include(c => c.Usuario).ThenInclude(u => u.Rol).AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim().ToLower();
            baseQ = baseQ.Where(c =>
                (c.Usuario != null && ((c.Usuario.Nombre != null && c.Usuario.Nombre.ToLower().Contains(term)) ||
                (c.Usuario.Apellido != null && c.Usuario.Apellido.ToLower().Contains(term)) ||
                (c.Usuario.Documento != null && c.Usuario.Documento.ToLower().Contains(term)) ||
                (c.Usuario.Correo != null && c.Usuario.Correo.ToLower().Contains(term)))) ||
                (c.Telefono != null && c.Telefono.ToLower().Contains(term)) ||
                (c.Direccion != null && c.Direccion.ToLower().Contains(term)) ||
                (c.Barrio != null && c.Barrio.ToLower().Contains(term)));
        }
        var totalCount = await baseQ.CountAsync();
        var items = await baseQ.OrderBy(c => c.Usuario.Nombre).Skip((page - 1) * pageSize).Take(pageSize)
            .Select(c => MapToDto(c)).ToListAsync();
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        return ServiceResult<object>.Ok(new { items, totalCount, page, pageSize, totalPages });
    }

    public async Task<ServiceResult<object>> GetByIdAsync(int id)
    {
        var cliente = await _context.Clientes.AsNoTracking().Include(c => c.Usuario).ThenInclude(u => u.Rol)
            .FirstOrDefaultAsync(c => c.Id == id);
        if (cliente == null) return ServiceResult<object>.NotFound();
        return ServiceResult<object>.Ok(MapToDto(cliente));
    }

    public async Task<ServiceResult<object>> GetSaldoDisponibleAsync(int id)
    {
        var existe = await _context.Clientes.AnyAsync(c => c.Id == id);
        if (!existe) return ServiceResult<object>.NotFound();
        var totalDevoluciones = await _context.Devoluciones
            .Where(d => d.ClienteId == id && (d.Estado == "Activo" || d.Estado == "Completada" || d.Estado == "Procesado"))
            .SumAsync(d => d.SaldoAFavor ?? 0);
        var totalUsado = await _context.Ventas
            .Where(v => v.ClienteId == id && v.Estado != "Anulada")
            .SumAsync(v => v.SaldoAFavorUsado ?? 0);
        var disponible = Math.Max(0, totalDevoluciones - totalUsado);
        return ServiceResult<object>.Ok(new { clienteId = id, totalDevoluciones, totalUsado, disponible });
    }

    public async Task<ServiceResult<object>> CreateAsync(ClienteInput input)
    {
        if (input == null) return ServiceResult<object>.Fail("El objeto cliente es requerido");
        if (string.IsNullOrWhiteSpace(input.Nombre)) return ServiceResult<object>.Fail("El nombre es requerido");
        if (string.IsNullOrWhiteSpace(input.Apellido)) return ServiceResult<object>.Fail("El apellido es requerido");
        if (string.IsNullOrWhiteSpace(input.Documento)) return ServiceResult<object>.Fail("El documento es requerido");
        if (string.IsNullOrWhiteSpace(input.Correo)) return ServiceResult<object>.Fail("El correo es requerido");

        var usuario = await _context.Usuarios.FindAsync(input.UsuarioId);
        if (usuario == null) return ServiceResult<object>.Fail("El usuario no existe");
        if (usuario.RolId != 3) return ServiceResult<object>.Fail("El usuario no tiene un rol de Cliente");
        if (await _context.Clientes.AnyAsync(c => c.UsuarioId == input.UsuarioId))
            return ServiceResult<object>.Fail("Ya existe un perfil de cliente para este usuario");
        if (await _context.Usuarios.AnyAsync(u => u.Documento == input.Documento))
            return ServiceResult<object>.Fail("El documento ya está registrado");
        if (!string.IsNullOrWhiteSpace(input.Correo) && await _context.Usuarios.AnyAsync(u => u.Correo == input.Correo))
            return ServiceResult<object>.Fail("Ya existe un usuario con ese correo");

        var cliente = new Cliente
        {
            UsuarioId = input.UsuarioId, Telefono = input.Telefono, Direccion = input.Direccion,
            Barrio = input.Barrio, FechaNacimiento = input.FechaNacimiento, Estado = input.Estado,
            FechaRegistro = DateTime.Now
        };
        _context.Clientes.Add(cliente);
        await _context.SaveChangesAsync();

        var clienteCreado = await _context.Clientes.Include(c => c.Usuario).ThenInclude(u => u.Rol)
            .FirstOrDefaultAsync(c => c.Id == cliente.Id);
        return ServiceResult<object>.Ok(MapToDto(clienteCreado!));
    }

    public async Task<ServiceResult<object>> UpdateAsync(int id, ClienteInput input)
    {
        if (input == null) return ServiceResult<object>.Fail("El objeto cliente es requerido");
        var clienteExistente = await _context.Clientes.Include(c => c.Usuario).FirstOrDefaultAsync(c => c.Id == id);
        if (clienteExistente == null) return ServiceResult<object>.NotFound();
        if (string.IsNullOrWhiteSpace(input.Nombre)) return ServiceResult<object>.Fail("El nombre es requerido");
        if (string.IsNullOrWhiteSpace(input.Apellido)) return ServiceResult<object>.Fail("El apellido es requerido");
        if (string.IsNullOrWhiteSpace(input.Documento)) return ServiceResult<object>.Fail("El documento es requerido");
        if (string.IsNullOrWhiteSpace(input.Correo)) return ServiceResult<object>.Fail("El correo es requerido");

        if (input.Documento != clienteExistente.Usuario.Documento)
            if (await _context.Usuarios.AnyAsync(u => u.Documento == input.Documento && u.Id != clienteExistente.UsuarioId))
                return ServiceResult<object>.Fail("Ya existe otro usuario con ese documento");
        if (input.Correo != clienteExistente.Usuario.Correo)
            if (await _context.Usuarios.AnyAsync(u => u.Correo == input.Correo && u.Id != clienteExistente.UsuarioId))
                return ServiceResult<object>.Fail("Ya existe otro usuario con ese correo");

        clienteExistente.Telefono = input.Telefono;
        clienteExistente.Direccion = input.Direccion;
        clienteExistente.Barrio = input.Barrio;
        clienteExistente.FechaNacimiento = input.FechaNacimiento;
        clienteExistente.Estado = input.Estado;

        var usuario = await _context.Usuarios.FindAsync(clienteExistente.UsuarioId);
        if (usuario != null)
        {
            if (!Helpers.ValidationHelper.ValidarUrlImagen(input.FotoPerfil, out var imgError))
                return ServiceResult<object>.Fail(imgError!);
            usuario.Nombre = input.Nombre; usuario.Apellido = input.Apellido;
            usuario.Documento = input.Documento; usuario.Correo = input.Correo;
            usuario.FotoPerfil = input.FotoPerfil;
        }
        await _context.SaveChangesAsync();
        return ServiceResult<object>.Ok(new { message = "Cliente actualizado" });
    }

    public async Task<ServiceResult<object>> CambiarEstadoAsync(int id, CambioEstadoBooleanInput input)
    {
        var cliente = await _context.Clientes.Include(c => c.Usuario).FirstOrDefaultAsync(c => c.Id == id);
        if (cliente == null) return ServiceResult<object>.NotFound();
        cliente.Estado = input.estado;
        if (cliente.Usuario != null) cliente.Usuario.Estado = input.estado;
        await _context.SaveChangesAsync();
        var dto = MapToDto(cliente);
        return ServiceResult<object>.Ok(new CambioEstadoResponse<ClienteDto>
        {
            entidad = dto, mensaje = input.estado ? "Cliente activado exitosamente" : "Cliente desactivado exitosamente", exitoso = true
        });
    }

    public async Task<ServiceResult<object>> DeleteAsync(int id)
    {
        var cliente = await _context.Clientes.Include(c => c.Agendamientos).Include(c => c.Venta).Include(c => c.Usuario)
            .FirstOrDefaultAsync(c => c.Id == id);
        if (cliente == null) return ServiceResult<object>.NotFound();
        var usuario = cliente.Usuario;
        bool tieneAgendamientosActivos = cliente.Agendamientos.Any(a => a.Estado != "Cancelada");
        bool tieneVentasCompletadas = cliente.Venta.Any(v => v.Estado == "Completada");
        bool tieneComprasUsuario = await _context.Compras.AnyAsync(c => c.UsuarioId == usuario.Id);
        bool tieneEntregasUsuario = await _context.EntregasInsumos.AnyAsync(e => e.UsuarioId == usuario.Id);
        bool tieneDevolucionesCliente = await _context.Devoluciones.AnyAsync(d => d.ClienteId == cliente.Id);

        if (tieneAgendamientosActivos || tieneVentasCompletadas || tieneComprasUsuario || tieneEntregasUsuario)
        {
            cliente.Estado = false;
            if (usuario != null) usuario.Estado = false;
            await _context.SaveChangesAsync();
            return ServiceResult<object>.Ok(new {
                message = "Cliente y usuario desactivados (historial asociado)", eliminado = true, fisico = false,
                motivos = new {
                    agendamientosActivos = cliente.Agendamientos.Count(a => a.Estado != "Cancelada"),
                    ventasCompletadas = cliente.Venta.Count(v => v.Estado == "Completada"),
                    comprasUsuario = tieneComprasUsuario, entregasUsuario = tieneEntregasUsuario,
                    devolucionesCliente = tieneDevolucionesCliente
                }
            });
        }
        _context.Agendamientos.RemoveRange(cliente.Agendamientos.Where(a => a.Estado == "Cancelada"));
        using var tx = await _context.Database.BeginTransactionAsync();
        try
        {
            if (usuario != null) _context.Usuarios.Remove(usuario);
            await _context.SaveChangesAsync();
            await tx.CommitAsync();
            return ServiceResult<object>.Ok(new { message = "Usuario y cliente eliminados físicamente", eliminado = true, fisico = true });
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            return ServiceResult<object>.Fail($"Error interno: {ex.Message}", 500);
        }
    }

    private static ClienteDto MapToDto(Cliente c) => new()
    {
        Id = c.Id, UsuarioId = c.UsuarioId, Nombre = c.Usuario?.Nombre, Apellido = c.Usuario?.Apellido,
        Documento = c.Usuario?.Documento, Correo = c.Usuario?.Correo, Telefono = c.Telefono,
        Direccion = c.Direccion, Barrio = c.Barrio, FechaNacimiento = c.FechaNacimiento,
        FotoPerfil = c.Usuario?.FotoPerfil, Estado = c.Estado, FechaRegistro = c.FechaRegistro,
        Usuario = c.Usuario == null ? null : new UsuarioDto
        {
            Id = c.Usuario.Id, Nombre = c.Usuario.Nombre, Apellido = c.Usuario.Apellido,
            Correo = c.Usuario.Correo, RolId = c.Usuario.RolId, RolNombre = c.Usuario.Rol?.Nombre,
            Estado = c.Usuario.Estado, FechaCreacion = c.Usuario.FechaCreacion
        }
    };
}
