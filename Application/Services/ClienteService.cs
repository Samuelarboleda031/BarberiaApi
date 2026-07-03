using BarberiaApi.Application.Common;
using BarberiaApi.Application.DTOs;
using BarberiaApi.Application.Interfaces;
using BarberiaApi.Domain.Constants;
using BarberiaApi.Domain.Entities;
using BarberiaApi.Infrastructure.Data;
using BarberiaApi.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using AutoMapper;
using AutoMapper.QueryableExtensions;

namespace BarberiaApi.Application.Services;

public class ClienteService : IClienteService
{
    private readonly BarberiaContext _context;
    private readonly IMapper _mapper;
    private readonly IDateTimeProvider _dt;
    private readonly IFirebaseAuthService _firebaseAuth;

    public ClienteService(BarberiaContext context, IMapper mapper, IDateTimeProvider dt, IFirebaseAuthService firebaseAuth)
    {
        _context = context;
        _mapper = mapper;
        _dt = dt;
        _firebaseAuth = firebaseAuth;
    }

    public async Task<ServiceResult<object>> GetAllAsync(int page, int pageSize, string? q)
    {
        PaginationHelper.Sanitize(ref page, ref pageSize);
        // Solo incluir clientes cuyo usuario tiene el rol "cliente" (previene perfiles huérfanos por cambio de rol)
        var clienteRoleId = await _context.Roles
            .Where(r => r.Nombre.ToLower() == RolesNombres.Cliente.ToLower())
            .Select(r => (int?)r.Id)
            .FirstOrDefaultAsync();
        var baseQ = _context.Clientes.AsNoTracking()
            .Where(c => c.Usuario != null && c.Usuario.RolId == clienteRoleId
                // Ocultar clientes anonimizados por baja logica (correo eliminado_{id}@baja.local).
                && (c.Usuario.Correo == null || !c.Usuario.Correo.EndsWith(UsuarioAnonimizado.DominioBaja)))
            .AsQueryable();
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
            .ProjectTo<ClienteDto>(_mapper.ConfigurationProvider).ToListAsync();

        // Calcular saldos disponibles en batch para los clientes de esta página
        if (items.Count > 0)
        {
            var clienteIds = items.Select(i => i.Id).ToList();

            var saldosDevoluciones = await _context.Devoluciones
                .Where(d => d.ClienteId.HasValue && clienteIds.Contains(d.ClienteId.Value) &&
                            (d.Estado == "Activo" || d.Estado == "Completada" || d.Estado == "Procesado"))
                .GroupBy(d => d.ClienteId!.Value)
                .Select(g => new { ClienteId = g.Key, Total = g.Sum(d => d.SaldoAFavor ?? 0) })
                .ToListAsync();

            var saldosUsados = await _context.Ventas
                .Where(v => v.ClienteId.HasValue && clienteIds.Contains(v.ClienteId.Value) && v.Estado != "Anulada")
                .GroupBy(v => v.ClienteId!.Value)
                .Select(g => new { ClienteId = g.Key, Total = g.Sum(v => v.SaldoAFavorUsado ?? 0) })
                .ToListAsync();

            var devMap = saldosDevoluciones.ToDictionary(x => x.ClienteId, x => x.Total);
            var usadoMap = saldosUsados.ToDictionary(x => x.ClienteId, x => x.Total);

            foreach (var item in items)
            {
                var devTotal = devMap.TryGetValue(item.Id, out var d) ? d : 0m;
                var usadoTotal = usadoMap.TryGetValue(item.Id, out var u) ? u : 0m;
                item.SaldoAFavor = Math.Max(0, devTotal - usadoTotal);
            }
        }

        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        return ServiceResult<object>.Ok(new { items, totalCount, page, pageSize, totalPages });
    }

    public async Task<ServiceResult<object>> GetByIdAsync(int id)
    {
        var cliente = await _context.Clientes.AsNoTracking()
            .ProjectTo<ClienteDto>(_mapper.ConfigurationProvider)
            .FirstOrDefaultAsync(c => c.Id == id);
        if (cliente == null) return ServiceResult<object>.NotFound();

        // Calcular saldo disponible
        var devTotal = await _context.Devoluciones
            .Where(d => d.ClienteId == id && (d.Estado == "Activo" || d.Estado == "Completada" || d.Estado == "Procesado"))
            .SumAsync(d => d.SaldoAFavor ?? 0);
        var usadoTotal = await _context.Ventas
            .Where(v => v.ClienteId == id && v.Estado != "Anulada")
            .SumAsync(v => v.SaldoAFavorUsado ?? 0);
        cliente.SaldoAFavor = Math.Max(0, devTotal - usadoTotal);

        return ServiceResult<object>.Ok(cliente);
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
        // NOTA: Validación estructural básica manejada por FluentValidation.

        var usuario = await _context.Usuarios
            .Include(u => u.Rol)
            .FirstOrDefaultAsync(u => u.Id == input.UsuarioId);
        if (usuario == null) return ServiceResult<object>.Fail("El usuario no existe");
        if (!string.Equals(usuario.Rol?.Nombre, RolesNombres.Cliente, StringComparison.OrdinalIgnoreCase))
            return ServiceResult<object>.Fail("El usuario no tiene un rol de Cliente");
        if (await _context.Clientes.AnyAsync(c => c.UsuarioId == input.UsuarioId))
            return ServiceResult<object>.Fail("Ya existe un perfil de cliente para este usuario");

        var cliente = new Cliente
        {
            UsuarioId = input.UsuarioId, Telefono = input.Telefono, Direccion = input.Direccion,
            Barrio = input.Barrio, FechaNacimiento = input.FechaNacimiento, Estado = input.Estado,
            FechaRegistro = _dt.NowColombia
        };
        _context.Clientes.Add(cliente);
        await _context.SaveChangesAsync();

        var dto = await _context.Clientes.AsNoTracking()
            .ProjectTo<ClienteDto>(_mapper.ConfigurationProvider)
            .FirstOrDefaultAsync(c => c.Id == cliente.Id);
            
        return ServiceResult<object>.Ok(dto!);
    }

    public async Task<ServiceResult<object>> UpdateAsync(int id, ClienteInput input)
    {
        var clienteExistente = await _context.Clientes.Include(c => c.Usuario).FirstOrDefaultAsync(c => c.Id == id);
        if (clienteExistente == null) return ServiceResult<object>.NotFound();
        
        // NOTA: Nombre, Apellido, Documento, Correo ya validados por FluentValidation.

        clienteExistente.Telefono = input.Telefono;
        clienteExistente.Direccion = input.Direccion;
        clienteExistente.Barrio = input.Barrio;
        clienteExistente.FechaNacimiento = input.FechaNacimiento;
        clienteExistente.Estado = input.Estado;

        var usuario = clienteExistente.Usuario;
        bool correoCambio = false;
        string correoAntiguo = string.Empty;
        if (usuario != null)
        {
            correoCambio = usuario.Correo != input.Correo;
            correoAntiguo = usuario.Correo;
            usuario.Nombre = input.Nombre; usuario.Apellido = input.Apellido;
            usuario.Documento = input.Documento; usuario.Correo = input.Correo;
            if (input.FotoPerfil != null)
                usuario.FotoPerfil = input.FotoPerfil;
        }
        await _context.SaveChangesAsync();

        // Si el correo cambió, actualizar en Firebase Authentication
        if (correoCambio)
        {
            await _firebaseAuth.UpdateUserEmailAsync(correoAntiguo, input.Correo);
        }

        return ServiceResult<object>.Ok(new { message = "Cliente actualizado" });
    }

    public async Task<ServiceResult<object>> CambiarEstadoAsync(int id, CambioEstadoBooleanInput input)
    {
        var cliente = await _context.Clientes.Include(c => c.Usuario).FirstOrDefaultAsync(c => c.Id == id);
        if (cliente == null) return ServiceResult<object>.NotFound();
        cliente.Estado = input.estado;
        if (cliente.Usuario != null) cliente.Usuario.Estado = input.estado;
        await _context.SaveChangesAsync();
        
        var dto = await _context.Clientes.AsNoTracking()
            .ProjectTo<ClienteDto>(_mapper.ConfigurationProvider)
            .FirstOrDefaultAsync(c => c.Id == id);
            
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
        bool tieneComprasUsuario = usuario != null && await _context.Compras.AnyAsync(c => c.UsuarioId == usuario.Id);

        if (tieneAgendamientosActivos || tieneVentasCompletadas || tieneComprasUsuario)
        {
            cliente.Estado = false;
            if (usuario != null) usuario.Estado = false;
            await _context.SaveChangesAsync();
            return ServiceResult<object>.Ok(new {
                message = "Cliente y usuario desactivados (historial asociado)", eliminado = true, fisico = false
            });
        }
        _context.Clientes.Remove(cliente);
        if (usuario != null) _context.Usuarios.Remove(usuario);
        await _context.SaveChangesAsync();
        return ServiceResult<object>.Ok(new { message = "Usuario y cliente eliminados físicamente", eliminado = true, fisico = true });
    }
}
