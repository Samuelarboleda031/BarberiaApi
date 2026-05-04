using BarberiaApi.Application.DTOs;
using BarberiaApi.Application.Interfaces;
using BarberiaApi.Domain.Entities;
using BarberiaApi.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using AutoMapper;
using AutoMapper.QueryableExtensions;

namespace BarberiaApi.Application.Services;

public class ProveedorService : IProveedorService
{
    private readonly BarberiaContext _context;
    private readonly IMapper _mapper;

    public ProveedorService(BarberiaContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    private async Task<ServiceResult<object>> GetByTipoAsync(string? tipo, int page, int pageSize, string? q)
    {
        if (page < 1) page = 1; if (pageSize < 1) pageSize = 5;
        var baseQ = _context.Proveedores.AsQueryable();
        if (tipo != null) baseQ = baseQ.Where(p => p.TipoProveedor == tipo);
        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim().ToLower();
            baseQ = baseQ.Where(p => (p.Nombre != null && p.Nombre.ToLower().Contains(term)) ||
                (p.NIT != null && p.NIT.ToLower().Contains(term)) || (p.RepresentanteLegal != null && p.RepresentanteLegal.ToLower().Contains(term)) ||
                (p.Correo != null && p.Correo.ToLower().Contains(term)) || (p.Telefono != null && p.Telefono.ToLower().Contains(term)));
        }
        var totalCount = await baseQ.CountAsync();
        var items = await baseQ.OrderBy(p => p.Nombre).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        return ServiceResult<object>.Ok(new { items, totalCount, page, pageSize, totalPages });
    }

    public Task<ServiceResult<object>> GetAllAsync(int page, int pageSize, string? q) => GetByTipoAsync(null, page, pageSize, q);
    public Task<ServiceResult<object>> GetNaturalesAsync(int page, int pageSize, string? q) => GetByTipoAsync("Natural", page, pageSize, q);
    public Task<ServiceResult<object>> GetJuridicosAsync(int page, int pageSize, string? q) => GetByTipoAsync("Juridico", page, pageSize, q);

    public async Task<ServiceResult<object>> GetByIdAsync(int id)
    {
        var proveedor = await _context.Proveedores.Include(p => p.Compras).FirstOrDefaultAsync(p => p.Id == id);
        if (proveedor == null) return ServiceResult<object>.NotFound();
        return ServiceResult<object>.Ok(proveedor);
    }

    public async Task<ServiceResult<object>> CreateNaturalAsync(ProveedorNaturalInput input)
    {
        if (await _context.Proveedores.AnyAsync(p => p.NIT == input.NIT)) return ServiceResult<object>.Fail("Ya existe un proveedor con ese NIT");
        var proveedor = new Proveedor
        {
            Nombre = input.Nombre,
            RepresentanteLegal = input.RepresentanteLegal,
            NumeroIdentificacion = input.NumeroIdentificacion,
            TipoIdentificacion = input.TipoIdentificacion,
            Correo = input.Correo,
            Telefono = input.Telefono,
            Direccion = input.Direccion,
            NIT = input.NIT,
            CorreoRepresentante = input.CorreoRepresentante,
            TelefonoRepresentante = input.TelefonoRepresentante,
            TipoProveedor = "Natural",
            Estado = true
        };
        _context.Proveedores.Add(proveedor); await _context.SaveChangesAsync();
        return ServiceResult<object>.Ok(proveedor);
    }

    public async Task<ServiceResult<object>> CreateJuridicoAsync(ProveedorJuridicoInput input)
    {
        if (await _context.Proveedores.AnyAsync(p => p.NIT == input.NIT)) return ServiceResult<object>.Fail("Ya existe un proveedor con ese NIT");
        var proveedor = new Proveedor
        {
            Nombre = input.Nombre,
            NIT = input.NIT,
            Correo = input.Correo,
            Telefono = input.Telefono,
            Direccion = input.Direccion,
            RepresentanteLegal = input.RepresentanteLegal,
            NumeroIdentificacion = input.NumeroIdentificacion,
            TipoIdentificacion = input.TipoIdentificacion,
            CorreoRepresentante = input.CorreoRepresentante,
            TelefonoRepresentante = input.TelefonoRepresentante,
            Ciudad = input.Ciudad,
            Departamento = input.Departamento,
            TipoProveedor = "Juridico",
            Estado = true
        };
        _context.Proveedores.Add(proveedor); await _context.SaveChangesAsync();
        return ServiceResult<object>.Ok(proveedor);
    }

    public async Task<ServiceResult<object>> CreateAsync(ProveedorCreateInput input)
    {
        var tipo = (input.TipoProveedor ?? "").Trim();
        if (tipo != "Natural" && tipo != "Juridico") return ServiceResult<object>.Fail("TipoProveedor debe ser 'Natural' o 'Juridico'");
        if (await _context.Proveedores.AnyAsync(p => p.NIT == input.NIT)) return ServiceResult<object>.Fail("Ya existe un proveedor con ese NIT");

        if (tipo == "Juridico")
        {
            if (string.IsNullOrWhiteSpace(input.RepresentanteLegal))
                return ServiceResult<object>.Fail("RepresentanteLegal es obligatorio para proveedores jurídicos");
            if (string.IsNullOrWhiteSpace(input.NumeroIdentificacion))
                return ServiceResult<object>.Fail("NumeroIdentificacion del representante es obligatorio");
            if (string.IsNullOrWhiteSpace(input.Ciudad))
                return ServiceResult<object>.Fail("Ciudad es obligatoria");
            if (string.IsNullOrWhiteSpace(input.Departamento))
                return ServiceResult<object>.Fail("Departamento es obligatorio");
        }

        var proveedor = new Proveedor
        {
            Nombre = input.Nombre,
            NIT = input.NIT,
            Correo = input.Correo,
            Telefono = input.Telefono,
            Direccion = input.Direccion,
            RepresentanteLegal = input.RepresentanteLegal,
            NumeroIdentificacion = input.NumeroIdentificacion,
            TipoIdentificacion = string.IsNullOrWhiteSpace(input.TipoIdentificacion) ? (tipo == "Natural" ? "CC" : "NIT") : input.TipoIdentificacion,
            CorreoRepresentante = input.CorreoRepresentante,
            TelefonoRepresentante = input.TelefonoRepresentante,
            Ciudad = input.Ciudad,
            Departamento = input.Departamento,
            TipoProveedor = tipo,
            Estado = true
        };
        _context.Proveedores.Add(proveedor); await _context.SaveChangesAsync();
        return ServiceResult<object>.Ok(proveedor);
    }

    public async Task<ServiceResult<object>> UpdateAsync(int id, ProveedorUpdateInput input)
    {
        var p = await _context.Proveedores.FindAsync(id);
        if (p == null) return ServiceResult<object>.NotFound();
        p.Nombre = input.Nombre;
        if (!string.IsNullOrWhiteSpace(input.NIT))
        {
            if (await _context.Proveedores.AnyAsync(x => x.NIT == input.NIT && x.Id != id)) return ServiceResult<object>.Fail("Ya existe un proveedor con ese NIT");
            p.NIT = input.NIT;
        }
        p.Correo = input.Correo; p.Telefono = input.Telefono; p.Direccion = input.Direccion;
        if (input.Estado.HasValue) p.Estado = input.Estado.Value;
        p.RepresentanteLegal = input.RepresentanteLegal;
        p.NumeroIdentificacion = input.NumeroIdentificacion;
        p.TipoIdentificacion = input.TipoIdentificacion;
        p.CorreoRepresentante = input.CorreoRepresentante;
        p.TelefonoRepresentante = input.TelefonoRepresentante;
        p.Ciudad = input.Ciudad;
        p.Departamento = input.Departamento;
        await _context.SaveChangesAsync();
        return ServiceResult<object>.Ok(new { message = "Proveedor actualizado" });
    }

    public async Task<ServiceResult<object>> CambiarEstadoAsync(int id, CambioEstadoBooleanInput input)
    {
        var proveedor = await _context.Proveedores.Include(p => p.Compras).FirstOrDefaultAsync(p => p.Id == id);
        if (proveedor == null) return ServiceResult<object>.NotFound();
        proveedor.Estado = input.estado; await _context.SaveChangesAsync();
        return ServiceResult<object>.Ok(new CambioEstadoResponse<Proveedor> { entidad = proveedor,
            mensaje = input.estado ? "Proveedor activado exitosamente" : "Proveedor desactivado exitosamente", exitoso = true });
    }

    public async Task<ServiceResult<object>> DeleteAsync(int id)
    {
        var proveedor = await _context.Proveedores.Include(p => p.Compras).FirstOrDefaultAsync(p => p.Id == id);
        if (proveedor == null) return ServiceResult<object>.NotFound();
        if (proveedor.Compras.Any())
        {
            proveedor.Estado = false; await _context.SaveChangesAsync();
            return ServiceResult<object>.Ok(new { message = "Proveedor desactivado (borrado lógico por tener compras asociadas)", eliminado = true, fisico = false, comprasAsociadas = proveedor.Compras.Count() });
        }
        _context.Proveedores.Remove(proveedor); await _context.SaveChangesAsync();
        return ServiceResult<object>.Ok(new { message = "Proveedor eliminado físicamente de la base de datos", eliminado = true, fisico = true });
    }
}
