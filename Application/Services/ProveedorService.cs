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
                (p.Identificacion != null && p.Identificacion.ToLower().Contains(term)) ||
                (p.RepresentanteLegal != null && p.RepresentanteLegal.ToLower().Contains(term)) ||
                (p.Correo != null && p.Correo.ToLower().Contains(term)) ||
                (p.Telefono != null && p.Telefono.ToLower().Contains(term)));
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
        if (await _context.Proveedores.AnyAsync(p => p.Identificacion == input.Identificacion))
            return ServiceResult<object>.Fail("Ya existe un proveedor con esa identificación");

        var tipoIdentProv = string.IsNullOrWhiteSpace(input.TipoIdentificacionProveedor) ? "CC" : input.TipoIdentificacionProveedor;

        // Para Natural: copiar datos del proveedor a los del representante automáticamente
        var proveedor = new Proveedor
        {
            TipoProveedor = "Natural",
            Nombre = input.Nombre,
            TipoIdentificacionProveedor = tipoIdentProv,
            Identificacion = input.Identificacion,
            Correo = input.Correo,
            Telefono = input.Telefono,
            Direccion = input.Direccion,
            Ciudad = input.Ciudad,
            Departamento = input.Departamento,
            // Copiar datos del proveedor al representante
            RepresentanteLegal = input.Nombre,
            TipoIdentificacionRepresentante = tipoIdentProv,
            IdentificacionRepresentante = input.Identificacion,
            CorreoRepresentante = input.Correo,
            TelefonoRepresentante = input.Telefono,
            Estado = true
        };
        _context.Proveedores.Add(proveedor); await _context.SaveChangesAsync();
        return ServiceResult<object>.Ok(proveedor);
    }

    public async Task<ServiceResult<object>> CreateJuridicoAsync(ProveedorJuridicoInput input)
    {
        if (await _context.Proveedores.AnyAsync(p => p.Identificacion == input.Identificacion))
            return ServiceResult<object>.Fail("Ya existe un proveedor con esa identificación");

        // Validaciones explícitas para Jurídico (todos los campos obligatorios)
        if (string.IsNullOrWhiteSpace(input.Direccion))
            return ServiceResult<object>.Fail("La dirección es obligatoria para proveedores jurídicos");
        if (string.IsNullOrWhiteSpace(input.Ciudad))
            return ServiceResult<object>.Fail("La ciudad es obligatoria para proveedores jurídicos");
        if (string.IsNullOrWhiteSpace(input.Departamento))
            return ServiceResult<object>.Fail("El departamento es obligatorio para proveedores jurídicos");
        if (string.IsNullOrWhiteSpace(input.RepresentanteLegal))
            return ServiceResult<object>.Fail("El representante legal es obligatorio para proveedores jurídicos");
        if (string.IsNullOrWhiteSpace(input.IdentificacionRepresentante))
            return ServiceResult<object>.Fail("La identificación del representante es obligatoria para proveedores jurídicos");
        if (string.IsNullOrWhiteSpace(input.CorreoRepresentante))
            return ServiceResult<object>.Fail("El correo del representante es obligatorio para proveedores jurídicos");
        if (string.IsNullOrWhiteSpace(input.TelefonoRepresentante))
            return ServiceResult<object>.Fail("El teléfono del representante es obligatorio para proveedores jurídicos");

        var proveedor = new Proveedor
        {
            TipoProveedor = "Juridico",
            Nombre = input.Nombre,
            TipoIdentificacionProveedor = string.IsNullOrWhiteSpace(input.TipoIdentificacionProveedor) ? "NIT" : input.TipoIdentificacionProveedor,
            Identificacion = input.Identificacion,
            Correo = input.Correo,
            Telefono = input.Telefono,
            Direccion = input.Direccion,
            Ciudad = input.Ciudad,
            Departamento = input.Departamento,
            RepresentanteLegal = input.RepresentanteLegal,
            TipoIdentificacionRepresentante = string.IsNullOrWhiteSpace(input.TipoIdentificacionRepresentante) ? "CC" : input.TipoIdentificacionRepresentante,
            IdentificacionRepresentante = input.IdentificacionRepresentante,
            CorreoRepresentante = input.CorreoRepresentante,
            TelefonoRepresentante = input.TelefonoRepresentante,
            Estado = true
        };
        _context.Proveedores.Add(proveedor); await _context.SaveChangesAsync();
        return ServiceResult<object>.Ok(proveedor);
    }

    public async Task<ServiceResult<object>> CreateAsync(ProveedorCreateInput input)
    {
        var tipo = (input.TipoProveedor ?? "").Trim();
        if (tipo != "Natural" && tipo != "Juridico")
            return ServiceResult<object>.Fail("TipoProveedor debe ser 'Natural' o 'Juridico'");
        if (await _context.Proveedores.AnyAsync(p => p.Identificacion == input.Identificacion))
            return ServiceResult<object>.Fail("Ya existe un proveedor con esa identificación");

        if (tipo == "Juridico")
        {
            if (string.IsNullOrWhiteSpace(input.Direccion))
                return ServiceResult<object>.Fail("La dirección es obligatoria para proveedores jurídicos");
            if (string.IsNullOrWhiteSpace(input.Ciudad))
                return ServiceResult<object>.Fail("La ciudad es obligatoria para proveedores jurídicos");
            if (string.IsNullOrWhiteSpace(input.Departamento))
                return ServiceResult<object>.Fail("El departamento es obligatorio para proveedores jurídicos");
            if (string.IsNullOrWhiteSpace(input.RepresentanteLegal))
                return ServiceResult<object>.Fail("El representante legal es obligatorio para proveedores jurídicos");
            if (string.IsNullOrWhiteSpace(input.IdentificacionRepresentante))
                return ServiceResult<object>.Fail("La identificación del representante es obligatoria para proveedores jurídicos");
            if (string.IsNullOrWhiteSpace(input.CorreoRepresentante))
                return ServiceResult<object>.Fail("El correo del representante es obligatorio para proveedores jurídicos");
            if (string.IsNullOrWhiteSpace(input.TelefonoRepresentante))
                return ServiceResult<object>.Fail("El teléfono del representante es obligatorio para proveedores jurídicos");
        }

        var tipoIdentProv = string.IsNullOrWhiteSpace(input.TipoIdentificacionProveedor)
            ? (tipo == "Natural" ? "CC" : "NIT")
            : input.TipoIdentificacionProveedor;

        // Para Natural: copiar datos del proveedor al representante
        var representanteLegal = tipo == "Natural" ? input.Nombre : input.RepresentanteLegal;
        var identificacionRepresentante = tipo == "Natural" ? input.Identificacion : input.IdentificacionRepresentante;
        var correoRepresentante = tipo == "Natural" ? input.Correo : input.CorreoRepresentante;
        var telefonoRepresentante = tipo == "Natural" ? input.Telefono : input.TelefonoRepresentante;
        var tipoIdentRep = tipo == "Natural"
            ? tipoIdentProv
            : (string.IsNullOrWhiteSpace(input.TipoIdentificacionRepresentante) ? "CC" : input.TipoIdentificacionRepresentante);

        var proveedor = new Proveedor
        {
            TipoProveedor = tipo,
            Nombre = input.Nombre,
            TipoIdentificacionProveedor = tipoIdentProv,
            Identificacion = input.Identificacion,
            Correo = input.Correo,
            Telefono = input.Telefono,
            Direccion = input.Direccion,
            Ciudad = input.Ciudad,
            Departamento = input.Departamento,
            RepresentanteLegal = representanteLegal,
            TipoIdentificacionRepresentante = tipoIdentRep,
            IdentificacionRepresentante = identificacionRepresentante,
            CorreoRepresentante = correoRepresentante,
            TelefonoRepresentante = telefonoRepresentante,
            Estado = true
        };
        _context.Proveedores.Add(proveedor); await _context.SaveChangesAsync();
        return ServiceResult<object>.Ok(proveedor);
    }

    public async Task<ServiceResult<object>> UpdateAsync(int id, ProveedorUpdateInput input)
    {
        var p = await _context.Proveedores.FindAsync(id);
        if (p == null) return ServiceResult<object>.NotFound();

        // Determinar el tipo de proveedor (el del input si viene, si no el actual)
        var tipo = !string.IsNullOrWhiteSpace(input.TipoProveedor) ? input.TipoProveedor!.Trim() : p.TipoProveedor;
        if (tipo != "Natural" && tipo != "Juridico")
            return ServiceResult<object>.Fail("TipoProveedor debe ser 'Natural' o 'Juridico'");

        // Validaciones de campos comunes obligatorios
        if (string.IsNullOrWhiteSpace(input.Nombre))
            return ServiceResult<object>.Fail("El nombre es obligatorio");
        if (string.IsNullOrWhiteSpace(input.Identificacion))
            return ServiceResult<object>.Fail("La identificación es obligatoria");
        if (string.IsNullOrWhiteSpace(input.Correo))
            return ServiceResult<object>.Fail("El correo es obligatorio");
        if (string.IsNullOrWhiteSpace(input.Telefono))
            return ServiceResult<object>.Fail("El teléfono es obligatorio");

        // Validaciones específicas para Jurídico
        if (tipo == "Juridico")
        {
            if (string.IsNullOrWhiteSpace(input.Direccion))
                return ServiceResult<object>.Fail("La dirección es obligatoria para proveedores jurídicos");
            if (string.IsNullOrWhiteSpace(input.Ciudad))
                return ServiceResult<object>.Fail("La ciudad es obligatoria para proveedores jurídicos");
            if (string.IsNullOrWhiteSpace(input.Departamento))
                return ServiceResult<object>.Fail("El departamento es obligatorio para proveedores jurídicos");
            if (string.IsNullOrWhiteSpace(input.RepresentanteLegal))
                return ServiceResult<object>.Fail("El representante legal es obligatorio para proveedores jurídicos");
            if (string.IsNullOrWhiteSpace(input.IdentificacionRepresentante))
                return ServiceResult<object>.Fail("La identificación del representante es obligatoria para proveedores jurídicos");
            if (string.IsNullOrWhiteSpace(input.CorreoRepresentante))
                return ServiceResult<object>.Fail("El correo del representante es obligatorio para proveedores jurídicos");
            if (string.IsNullOrWhiteSpace(input.TelefonoRepresentante))
                return ServiceResult<object>.Fail("El teléfono del representante es obligatorio para proveedores jurídicos");
        }

        // Validar identificación duplicada
        if (await _context.Proveedores.AnyAsync(x => x.Identificacion == input.Identificacion && x.Id != id))
            return ServiceResult<object>.Fail("Ya existe un proveedor con esa identificación");

        var tipoIdentProv = string.IsNullOrWhiteSpace(input.TipoIdentificacionProveedor)
            ? (tipo == "Natural" ? "CC" : "NIT")
            : input.TipoIdentificacionProveedor;

        // Asignación de campos
        p.TipoProveedor = tipo;
        p.Nombre = input.Nombre;
        p.TipoIdentificacionProveedor = tipoIdentProv;
        p.Identificacion = input.Identificacion;
        p.Correo = input.Correo;
        p.Telefono = input.Telefono;
        p.Direccion = input.Direccion;
        p.Ciudad = input.Ciudad;
        p.Departamento = input.Departamento;

        // Para Natural: copiar datos del proveedor al representante
        if (tipo == "Natural")
        {
            p.RepresentanteLegal = input.Nombre;
            p.TipoIdentificacionRepresentante = tipoIdentProv;
            p.IdentificacionRepresentante = input.Identificacion;
            p.CorreoRepresentante = input.Correo;
            p.TelefonoRepresentante = input.Telefono;
        }
        else
        {
            p.RepresentanteLegal = input.RepresentanteLegal;
            p.TipoIdentificacionRepresentante = string.IsNullOrWhiteSpace(input.TipoIdentificacionRepresentante)
                ? "CC"
                : input.TipoIdentificacionRepresentante;
            p.IdentificacionRepresentante = input.IdentificacionRepresentante;
            p.CorreoRepresentante = input.CorreoRepresentante;
            p.TelefonoRepresentante = input.TelefonoRepresentante;
        }

        if (input.Estado.HasValue) p.Estado = input.Estado.Value;

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
