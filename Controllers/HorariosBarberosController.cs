using BarberiaApi.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BarberiaApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class HorariosBarberosController : ControllerBase
    {
        private readonly BarberiaContext _context;

        public HorariosBarberosController(BarberiaContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<HorariosBarbero>>> GetAll()
        {
            return await _context.HorariosBarberos
                .Include(h => h.Barbero)
                .ToListAsync();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<HorariosBarbero>> GetById(int id)
        {
            var horario = await _context.HorariosBarberos
                .Include(h => h.Barbero)
                .FirstOrDefaultAsync(h => h.Id == id);

            if (horario == null) return NotFound();
            return Ok(horario);
        }

        [HttpGet("barbero/{barberoId}")]
        public async Task<ActionResult<IEnumerable<HorariosBarbero>>> GetByBarbero(int barberoId)
        {
            var horarios = await _context.HorariosBarberos
                .Include(h => h.Barbero)
                .Where(h => h.BarberoId == barberoId && h.Estado == true)
                .OrderBy(h => h.DiaSemana)
                .ToListAsync();

            return Ok(horarios);
        }

        [HttpPost]
        public async Task<ActionResult<HorariosBarbero>> Create([FromBody] HorarioBarberoCreateInput input)
        {
            if (input == null)
                return BadRequest("Los datos del horario son requeridos");

            // Validar que el barbero exista
            var barbero = await _context.Barberos.FindAsync(input.BarberoId);
            if (barbero == null)
                return BadRequest("El barbero especificado no existe");

            // Validar que no exista un horario para el mismo día y barbero
            var horarioExistente = await _context.HorariosBarberos
                .FirstOrDefaultAsync(h => h.BarberoId == input.BarberoId && h.DiaSemana == input.DiaSemana);

            if (horarioExistente != null)
                return BadRequest("Ya existe un horario para este barbero en el día especificado");

            var horario = new HorariosBarbero
            {
                BarberoId = input.BarberoId,
                DiaSemana = input.DiaSemana,
                HoraInicio = input.HoraInicio,
                HoraFin = input.HoraFin,
                Estado = true
            };

            _context.HorariosBarberos.Add(horario);
            await _context.SaveChangesAsync();

            // Retornar el horario creado con el barbero incluido
            await _context.Entry(horario).Reference(h => h.Barbero).LoadAsync();

            return CreatedAtAction(nameof(GetById), new { id = horario.Id }, horario);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<HorariosBarbero>> Update(int id, [FromBody] HorarioBarberoUpdateInput input)
        {
            var horario = await _context.HorariosBarberos.FindAsync(id);
            if (horario == null) return NotFound();

            // Validar que el barbero exista si se está cambiando
            if (input.BarberoId.HasValue)
            {
                var barbero = await _context.Barberos.FindAsync(input.BarberoId.Value);
                if (barbero == null)
                    return BadRequest("El barbero especificado no existe");

                // Validar que no exista un horario para el mismo día y barbero (si cambia el día o el barbero)
                if ((input.DiaSemana.HasValue && input.DiaSemana.Value != horario.DiaSemana) ||
                    (input.BarberoId.HasValue && input.BarberoId.Value != horario.BarberoId))
                {
                    var horarioExistente = await _context.HorariosBarberos
                        .FirstOrDefaultAsync(h => h.BarberoId == input.BarberoId.Value && 
                                               h.DiaSemana == (input.DiaSemana ?? horario.DiaSemana) &&
                                               h.Id != id);

                    if (horarioExistente != null)
                        return BadRequest("Ya existe un horario para este barbero en el día especificado");
                }
            }

            // Actualizar solo los campos proporcionados
            if (input.BarberoId.HasValue) horario.BarberoId = input.BarberoId.Value;
            if (input.DiaSemana.HasValue) horario.DiaSemana = input.DiaSemana.Value;
            if (input.HoraInicio.HasValue) horario.HoraInicio = input.HoraInicio.Value;
            if (input.HoraFin.HasValue) horario.HoraFin = input.HoraFin.Value;
            if (input.Estado.HasValue) horario.Estado = input.Estado.Value;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _context.HorariosBarberos.AnyAsync(e => e.Id == id))
                    return NotFound();
                throw;
            }

            // Retornar el horario actualizado con el barbero incluido
            await _context.Entry(horario).Reference(h => h.Barbero).LoadAsync();

            return Ok(horario);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var horario = await _context.HorariosBarberos.FindAsync(id);
            if (horario == null) return NotFound();

            _context.HorariosBarberos.Remove(horario);
            await _context.SaveChangesAsync();

            return Ok(new { 
                message = "Horario eliminado exitosamente", 
                eliminado = true,
                barberoId = horario.BarberoId,
                diaSemana = horario.DiaSemana
            });
        }

        [HttpPost("{id}/estado")]
        public async Task<ActionResult<CambioEstadoResponse<HorariosBarbero>>> CambiarEstado(int id, [FromBody] CambioEstadoBooleanInput input)
        {
            var horario = await _context.HorariosBarberos
                .Include(h => h.Barbero)
                .FirstOrDefaultAsync(h => h.Id == id);

            if (horario == null) return NotFound();

            horario.Estado = input.estado;
            await _context.SaveChangesAsync();

            var response = new CambioEstadoResponse<HorariosBarbero>
            {
                entidad = horario,
                mensaje = input.estado ? "Horario activado exitosamente" : "Horario desactivado exitosamente",
                exitoso = true
            };

            return Ok(response);
        }

        [HttpGet("disponibles/{fecha}")]
        public async Task<ActionResult<IEnumerable<object>>> GetDisponibles(string fecha)
        {
            if (!DateTime.TryParse(fecha, out DateTime fechaConsulta))
                return BadRequest("Formato de fecha inválido");

            var diaSemana = (int)fechaConsulta.DayOfWeek;
            // Ajustar para que Lunes = 1, Domingo = 7 (en .NET Sunday = 0)
            if (diaSemana == 0) diaSemana = 7;

            var horariosDisponibles = await _context.HorariosBarberos
                .Include(h => h.Barbero)
                    .ThenInclude(b => b.Usuario)
                .Where(h => h.Estado == true && 
                           h.DiaSemana == diaSemana &&
                           h.Barbero.Estado == true)
                .Select(h => new
                {
                    id = h.Id,
                    barberoId = h.BarberoId,
                    barberoNombre = h.Barbero.Usuario.Nombre + " " + h.Barbero.Usuario.Apellido,
                    diaSemana = h.DiaSemana,
                    horaInicio = h.HoraInicio.ToString(@"hh\:mm"),
                    horaFin = h.HoraFin.ToString(@"hh\:mm")
                })
                .ToListAsync();

            return Ok(horariosDisponibles);
        }
    }

    // DTOs para HorariosBarbero
    public class HorarioBarberoCreateInput
    {
        public int BarberoId { get; set; }
        public int DiaSemana { get; set; } // 1=Lunes, 7=Domingo
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
}
