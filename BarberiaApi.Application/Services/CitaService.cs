using BarberiaApi.Application.DTOs;
using BarberiaApi.Application.Interfaces;
using BarberiaApi.Domain.Entities;
using BarberiaApi.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BarberiaApi.Application.Services;

public class CitaService : ICitaService
{
    private readonly BarberiaContext _context;

    public CitaService(BarberiaContext context)
    {
        _context = context;
    }

    public async Task<ServiceResult<object>> GetAllAsync(int page, int pageSize, string? q)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 100;
        if (pageSize > 300) pageSize = 300;
        var baseQ = _context.Agendamientos
            .Include(a => a.Cliente).ThenInclude(c => c.Usuario)
            .Include(a => a.Barbero).ThenInclude(b => b.Usuario)
            .Include(a => a.Servicio)
            .Include(a => a.Paquete)
            .AsNoTracking()
            .AsSplitQuery()
            .AsQueryable();
        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim().ToLower();
            baseQ = baseQ.Where(a =>
                (a.Estado != null && a.Estado.ToLower().Contains(term)) ||
                (a.Cliente != null && a.Cliente.Usuario != null && (
                    (a.Cliente.Usuario.Nombre != null && a.Cliente.Usuario.Nombre.ToLower().Contains(term)) ||
                    (a.Cliente.Usuario.Apellido != null && a.Cliente.Usuario.Apellido.ToLower().Contains(term))
                )) ||
                (a.Barbero != null && a.Barbero.Usuario != null && (
                    (a.Barbero.Usuario.Nombre != null && a.Barbero.Usuario.Nombre.ToLower().Contains(term)) ||
                    (a.Barbero.Usuario.Apellido != null && a.Barbero.Usuario.Apellido.ToLower().Contains(term))
                )) ||
                (a.Servicio != null && a.Servicio.Nombre != null && a.Servicio.Nombre.ToLower().Contains(term)) ||
                (a.Paquete != null && a.Paquete.Nombre != null && a.Paquete.Nombre.ToLower().Contains(term))
            );
        }
        var ags = await baseQ
            .OrderByDescending(a => a.FechaHora)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
        var items = ags.Select(a => new CitaFrontend
        {
            id = a.Id,
            cliente = a.Cliente?.Usuario?.Nombre + " " + a.Cliente?.Usuario?.Apellido ?? "Cliente",
            telefono = a.Cliente?.Telefono ?? "",
            servicio = a.Servicio?.Nombre ?? a.Paquete?.Nombre ?? "Servicio",
            barbero = a.Barbero?.Usuario?.Nombre + " " + a.Barbero?.Usuario?.Apellido ?? "Barbero",
            fecha = a.FechaHora.ToString("yyyy-MM-dd"),
            hora = a.FechaHora.ToString("HH:mm"),
            duracion = a.Servicio?.DuracionMinutes ?? a.Paquete?.DuracionMinutos ?? 30,
            precio = a.Servicio?.Precio ?? a.Paquete?.Precio ?? 0,
            estado = a.Estado ?? "Pendiente",
            notas = a.Notas ?? ""
        }).ToList();
        return ServiceResult<object>.Ok(items);
    }

    public async Task<ServiceResult<object>> GetByIdAsync(int id)
    {
        var agendamiento = await _context.Agendamientos
            .Include(a => a.Cliente).ThenInclude(c => c.Usuario)
            .Include(a => a.Barbero).ThenInclude(b => b.Usuario)
            .Include(a => a.Servicio)
            .Include(a => a.Paquete)
            .AsNoTracking()
            .AsSplitQuery()
            .FirstOrDefaultAsync(a => a.Id == id);

        if (agendamiento == null) return ServiceResult<object>.NotFound();

        var cita = new CitaFrontend
        {
            id = agendamiento.Id,
            cliente = agendamiento.Cliente?.Usuario?.Nombre + " " + agendamiento.Cliente?.Usuario?.Apellido ?? "Cliente",
            telefono = agendamiento.Cliente?.Telefono ?? "",
            servicio = agendamiento.Servicio?.Nombre ?? agendamiento.Paquete?.Nombre ?? "Servicio",
            barbero = agendamiento.Barbero?.Usuario?.Nombre + " " + agendamiento.Barbero?.Usuario?.Apellido ?? "Barbero",
            fecha = agendamiento.FechaHora.ToString("yyyy-MM-dd"),
            hora = agendamiento.FechaHora.ToString("HH:mm"),
            duracion = agendamiento.Servicio?.DuracionMinutes ?? agendamiento.Paquete?.DuracionMinutos ?? 30,
            precio = agendamiento.Servicio?.Precio ?? agendamiento.Paquete?.Precio ?? 0,
            estado = agendamiento.Estado ?? "Pendiente",
            notas = agendamiento.Notas ?? ""
        };

        return ServiceResult<object>.Ok(cita);
    }

    public async Task<ServiceResult<object>> CreateAsync(CitaInputFrontend input)
    {
        if (input == null) return ServiceResult<object>.Fail("Los datos de la cita son requeridos");

        Cliente? cliente = null;
        if (!string.IsNullOrEmpty(input.cliente))
        {
            var nombreCliente = input.cliente.Split(' ')[0];
            cliente = await _context.Clientes
                .Include(c => c.Usuario)
                .FirstOrDefaultAsync(c => c.Usuario.Nombre.Contains(nombreCliente) &&
                                           c.Telefono == input.telefono);

            if (cliente == null)
            {
                var nombres = input.cliente.Split(' ');
                var nuevoUsuario = new Usuario
                {
                    Nombre = nombres.Length > 0 ? nombres[0] : input.cliente,
                    Apellido = nombres.Length > 1 ? string.Join(" ", nombres.Skip(1)) : "",
                    Correo = $"{nombres[0].ToLower()}@temp.com",
                    Contrasena = "temp123",
                    RolId = 3,
                    Estado = true,
                    FechaCreacion = DateTime.Now
                };
                _context.Usuarios.Add(nuevoUsuario);
                await _context.SaveChangesAsync();

                cliente = new Cliente
                {
                    UsuarioId = nuevoUsuario.Id,
                    Telefono = input.telefono,
                    Estado = true,
                    FechaRegistro = DateTime.Now
                };
                _context.Clientes.Add(cliente);
                await _context.SaveChangesAsync();
            }
        }

        var barbero = await _context.Barberos
            .Include(b => b.Usuario)
            .FirstOrDefaultAsync(b => (b.Usuario.Nombre + " " + b.Usuario.Apellido).Contains(input.barbero));

        if (barbero == null) return ServiceResult<object>.Fail("Barbero no encontrado");

        Servicio? servicio = null;
        Paquete? paquete = null;
        decimal precio = 0;
        int duracion = 30;

        servicio = await _context.Servicios.FirstOrDefaultAsync(s => s.Nombre == input.servicio);
        if (servicio != null)
        {
            precio = servicio.Precio;
            duracion = servicio.DuracionMinutes ?? 30;
        }
        else
        {
            paquete = await _context.Paquetes.FirstOrDefaultAsync(p => p.Nombre == input.servicio);
            if (paquete != null)
            {
                precio = paquete.Precio;
                duracion = paquete.DuracionMinutos;
            }
        }

        if (servicio == null && paquete == null) return ServiceResult<object>.Fail("Servicio no encontrado");

        if (!DateTime.TryParse($"{input.fecha} {input.hora}", out DateTime fechaHora))
            return ServiceResult<object>.Fail("Formato de fecha u hora inválido");

        if (!barbero.Estado || !barbero.Usuario.Estado)
            return ServiceResult<object>.Fail("El barbero seleccionado no está activo.");

        var diaSemana = (int)fechaHora.DayOfWeek;
        if (diaSemana == 0) diaSemana = 7;

        var horario = await _context.HorariosBarberos
            .FirstOrDefaultAsync(h => h.BarberoId == barbero.Id && h.DiaSemana == diaSemana && h.Estado == true);

        if (horario == null)
            return ServiceResult<object>.Fail("El barbero no trabaja en este día.");

        var horaCita = fechaHora.TimeOfDay;
        var horaFinCita = horaCita.Add(TimeSpan.FromMinutes(duracion));

        if (horaCita < horario.HoraInicio || horaFinCita > horario.HoraFin)
            return ServiceResult<object>.Fail($"La cita está fuera del horario de trabajo del barbero ({horario.HoraInicio:hh\\:mm} - {horario.HoraFin:hh\\:mm}).");

        var horaFin = fechaHora.AddMinutes(duracion);
        var existeTraslape = await _context.Agendamientos.AnyAsync(a =>
            a.BarberoId == barbero.Id &&
            a.FechaHora < horaFin &&
            a.FechaHora.AddMinutes(duracion) > fechaHora &&
            a.Estado != "Cancelada");

        if (existeTraslape)
            return ServiceResult<object>.Fail("El barbero ya tiene una cita programada en ese horario.");

        var agendamiento = new Agendamiento
        {
            ClienteId = cliente?.Id ?? 0,
            BarberoId = barbero.Id,
            ServicioId = servicio?.Id,
            PaqueteId = paquete?.Id,
            FechaHora = fechaHora,
            Notas = input.notas,
            Estado = input.estado ?? "Pendiente"
        };

        _context.Agendamientos.Add(agendamiento);
        await _context.SaveChangesAsync();

        var citaResponse = new CitaFrontend
        {
            id = agendamiento.Id,
            cliente = cliente?.Usuario?.Nombre + " " + cliente?.Usuario?.Apellido ?? input.cliente,
            telefono = cliente?.Telefono ?? input.telefono,
            servicio = servicio?.Nombre ?? paquete?.Nombre ?? input.servicio,
            barbero = barbero.Usuario.Nombre + " " + barbero.Usuario.Apellido,
            fecha = agendamiento.FechaHora.ToString("yyyy-MM-dd"),
            hora = agendamiento.FechaHora.ToString("HH:mm"),
            duracion = duracion,
            precio = precio,
            estado = agendamiento.Estado ?? "Pendiente",
            notas = agendamiento.Notas ?? ""
        };

        return ServiceResult<object>.Ok(citaResponse);
    }

    public async Task<ServiceResult<object>> UpdateAsync(int id, CitaInputFrontend input)
    {
        var agendamiento = await _context.Agendamientos.FindAsync(id);
        if (agendamiento == null) return ServiceResult<object>.NotFound();

        var nombreCliente = input.cliente.Split(' ')[0];
        var cliente = await _context.Clientes
            .Include(c => c.Usuario)
            .FirstOrDefaultAsync(c => c.Usuario.Nombre.Contains(nombreCliente) &&
                                       c.Telefono == input.telefono);

        var barbero = await _context.Barberos
            .Include(b => b.Usuario)
            .FirstOrDefaultAsync(b => (b.Usuario.Nombre + " " + b.Usuario.Apellido).Contains(input.barbero));

        if (barbero == null) return ServiceResult<object>.Fail("Barbero no encontrado");

        Servicio? servicio = null;
        Paquete? paquete = null;
        decimal precio = 0;
        int duracion = 30;

        servicio = await _context.Servicios.FirstOrDefaultAsync(s => s.Nombre == input.servicio);
        if (servicio != null)
        {
            precio = servicio.Precio;
            duracion = servicio.DuracionMinutes ?? 30;
        }
        else
        {
            paquete = await _context.Paquetes.FirstOrDefaultAsync(p => p.Nombre == input.servicio);
            if (paquete != null)
            {
                precio = paquete.Precio;
                duracion = paquete.DuracionMinutos;
            }
        }

        if (servicio == null && paquete == null) return ServiceResult<object>.Fail("Servicio no encontrado");

        if (!DateTime.TryParse($"{input.fecha} {input.hora}", out DateTime fechaHora))
            return ServiceResult<object>.Fail("Formato de fecha u hora inválido");

        if (!barbero.Estado || !barbero.Usuario.Estado)
            return ServiceResult<object>.Fail("El barbero seleccionado no está activo.");

        var diaSemana = (int)fechaHora.DayOfWeek;
        if (diaSemana == 0) diaSemana = 7;

        var horario = await _context.HorariosBarberos
            .FirstOrDefaultAsync(h => h.BarberoId == barbero.Id && h.DiaSemana == diaSemana && h.Estado == true);

        if (horario == null)
            return ServiceResult<object>.Fail("El barbero no trabaja en este día.");

        var horaCita = fechaHora.TimeOfDay;
        var horaFinCita = horaCita.Add(TimeSpan.FromMinutes(duracion));

        if (horaCita < horario.HoraInicio || horaFinCita > horario.HoraFin)
            return ServiceResult<object>.Fail($"La cita está fuera del horario de trabajo del barbero ({horario.HoraInicio:hh\\:mm} - {horario.HoraFin:hh\\:mm}).");

        agendamiento.ClienteId = cliente?.Id ?? agendamiento.ClienteId;
        agendamiento.BarberoId = barbero.Id;
        agendamiento.ServicioId = servicio?.Id;
        agendamiento.PaqueteId = paquete?.Id;
        agendamiento.FechaHora = fechaHora;
        agendamiento.Notas = input.notas;
        agendamiento.Estado = input.estado ?? agendamiento.Estado;

        await _context.SaveChangesAsync();

        var citaResponse = new CitaFrontend
        {
            id = agendamiento.Id,
            cliente = cliente?.Usuario?.Nombre + " " + cliente?.Usuario?.Apellido ?? input.cliente,
            telefono = cliente?.Telefono ?? input.telefono,
            servicio = servicio?.Nombre ?? paquete?.Nombre ?? input.servicio,
            barbero = barbero.Usuario.Nombre + " " + barbero.Usuario.Apellido,
            fecha = agendamiento.FechaHora.ToString("yyyy-MM-dd"),
            hora = agendamiento.FechaHora.ToString("HH:mm"),
            duracion = duracion,
            precio = precio,
            estado = agendamiento.Estado ?? "Pendiente",
            notas = agendamiento.Notas ?? ""
        };

        return ServiceResult<object>.Ok(citaResponse);
    }

    public async Task<ServiceResult<object>> CambiarEstadoAsync(int id, CambioEstadoInput input)
    {
        var agendamiento = await _context.Agendamientos
            .Include(a => a.Cliente).ThenInclude(c => c.Usuario)
            .Include(a => a.Barbero).ThenInclude(b => b.Usuario)
            .Include(a => a.Servicio)
            .Include(a => a.Paquete)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (agendamiento == null) return ServiceResult<object>.NotFound();

        if (string.IsNullOrWhiteSpace(input.estado))
            return ServiceResult<object>.Fail("El estado es requerido");

        agendamiento.Estado = input.estado;
        await _context.SaveChangesAsync();

        var citaResponse = new CitaFrontend
        {
            id = agendamiento.Id,
            cliente = agendamiento.Cliente?.Usuario?.Nombre + " " + agendamiento.Cliente?.Usuario?.Apellido ?? "Cliente",
            telefono = agendamiento.Cliente?.Telefono ?? "",
            servicio = agendamiento.Servicio?.Nombre ?? agendamiento.Paquete?.Nombre ?? "Servicio",
            barbero = agendamiento.Barbero?.Usuario?.Nombre + " " + agendamiento.Barbero?.Usuario?.Apellido ?? "Barbero",
            fecha = agendamiento.FechaHora.ToString("yyyy-MM-dd"),
            hora = agendamiento.FechaHora.ToString("HH:mm"),
            duracion = agendamiento.Servicio?.DuracionMinutes ?? agendamiento.Paquete?.DuracionMinutos ?? 30,
            precio = agendamiento.Servicio?.Precio ?? agendamiento.Paquete?.Precio ?? 0,
            estado = agendamiento.Estado ?? "Pendiente",
            notas = agendamiento.Notas ?? ""
        };

        return ServiceResult<object>.Ok(citaResponse);
    }

    public async Task<ServiceResult<object>> DeleteAsync(int id)
    {
        var agendamiento = await _context.Agendamientos
            .Include(a => a.Cliente).ThenInclude(c => c.Usuario)
            .Include(a => a.Barbero).ThenInclude(b => b.Usuario)
            .Include(a => a.Servicio)
            .Include(a => a.Paquete)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (agendamiento == null) return ServiceResult<object>.NotFound();

        agendamiento.Estado = "Cancelada";
        await _context.SaveChangesAsync();

        return ServiceResult<object>.Ok(new
        {
            message = "Cita cancelada (borrado lógico)",
            eliminado = true,
            fisico = false,
            clienteAsociado = agendamiento.Cliente?.Usuario?.Nombre + " " + agendamiento.Cliente?.Usuario?.Apellido,
            barberoAsociado = agendamiento.Barbero?.Usuario?.Nombre + " " + agendamiento.Barbero?.Usuario?.Apellido,
            servicioAsociado = agendamiento.Servicio?.Nombre,
            paqueteAsociado = agendamiento.Paquete?.Nombre
        });
    }
}
