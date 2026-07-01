using BarberiaApi.Domain.Entities;
using BarberiaApi.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace BarberiaApi.Infrastructure.Services;

public class NotificacionCitasService : INotificacionCitasService
{
    private readonly IEmailProxyService _emailProxy;
    private readonly IWhatsAppService _whatsApp;
    private readonly IConfiguration _configuration;
    private readonly ILogger<NotificacionCitasService> _logger;
    private readonly BarberiaContext _context;

    public NotificacionCitasService(
        IEmailProxyService emailProxy,
        IWhatsAppService whatsApp,
        IConfiguration configuration,
        ILogger<NotificacionCitasService> logger,
        BarberiaContext context)
    {
        _emailProxy = emailProxy;
        _whatsApp = whatsApp;
        _configuration = configuration;
        _logger = logger;
        _context = context;
    }

    public async Task<ResultadoNotificacionCita> NotificarCancelacionPorDesactivacionAsync(
        Agendamiento agendamiento,
        string motivo,
        IReadOnlyCollection<DateTime> sugerenciasReprogramacion)
    {
        var correo = agendamiento.Cliente?.Usuario?.Correo;
        ResultadoNotificacionCita emailResult;

        if (string.IsNullOrWhiteSpace(correo))
        {
            emailResult = new ResultadoNotificacionCita { Enviado = false, Canal = "smtp", Mensaje = "Cliente sin correo registrado." };
        }
        else
        {
            var request = new CancelacionEmailProxyRequest
            {
                ClienteNombre = NombreCliente(agendamiento),
                ClienteEmail = correo,
                BarberoNombre = NombreBarbero(agendamiento),
                FechaOriginal = agendamiento.FechaHora.ToString("o"),
                Motivo = motivo,
                SugerenciasReprogramacion = sugerenciasReprogramacion.Select(s => s.ToString("o")).ToList()
            };
            var resultado = await _emailProxy.EnviarCancelacionAsync(request);
            emailResult = new ResultadoNotificacionCita
            {
                Enviado = resultado.Enviado,
                Canal = "smtp",
                Mensaje = resultado.Mensaje
            };
        }

        await EnviarWhatsAppCancelacionAsync(agendamiento, motivo);

        return emailResult;
    }

    public async Task<ResultadoNotificacionCita> NotificarCancelacionGeneralAsync(
        Agendamiento agendamiento,
        string motivo)
    {
        return await NotificarCancelacionPorDesactivacionAsync(agendamiento, motivo, Array.Empty<DateTime>());
    }

    public Task NotificarCancelacionWhatsAppAsync(Agendamiento agendamiento, string motivo)
        => EnviarWhatsAppCancelacionAsync(agendamiento, motivo);

    public async Task NotificarCreacionAsync(Agendamiento agendamiento)
    {
        if (!_whatsApp.EstaHabilitado) return;

        var cliente = NombreCliente(agendamiento);
        var barbero = NombreBarbero(agendamiento);
        var fecha = FechaHora(agendamiento);
        var servicio = Servicio(agendamiento);

        var msgCliente =
            $"✂️ *{AppName}*\n\nHola {cliente}, tu cita quedó agendada ✅\n\n" +
            $"👤 Barbero: {barbero}\n💈 Servicio: {servicio}\n📅 {fecha}\n\n¡Te esperamos!";
        var msgBarbero =
            $"✂️ *{AppName}*\n\nHola {barbero}, tienes una nueva cita asignada 📅\n\n" +
            $"👤 Cliente: {cliente}\n💈 Servicio: {servicio}\n📅 {fecha}";
        var msgAdmin =
            $"✂️ *{AppName}*\n\nNueva cita registrada 📅\n\n" +
            $"👤 Cliente: {cliente}\n👤 Barbero: {barbero}\n💈 Servicio: {servicio}\n📅 {fecha}";

        await EnviarACliente(agendamiento, msgCliente, "creación cita");
        await EnviarABarbero(agendamiento, msgBarbero, "creación cita");
        await EnviarAAdmin(msgAdmin, "creación cita");
    }

    public async Task NotificarRecordatorioAsync(Agendamiento agendamiento)
    {
        if (!_whatsApp.EstaHabilitado) return;

        var cliente = NombreCliente(agendamiento);
        var barbero = NombreBarbero(agendamiento);
        var fecha = FechaHora(agendamiento);
        var servicio = Servicio(agendamiento);

        var msgCliente =
            $"⏰ *{AppName}*\n\nHola {cliente}, te recordamos tu cita próxima ⏰\n\n" +
            $"👤 Barbero: {barbero}\n💈 Servicio: {servicio}\n📅 {fecha}\n\n¡Te esperamos pronto!";
        var msgBarbero =
            $"⏰ *{AppName}*\n\nHola {barbero}, recordatorio de cita próxima ⏰\n\n" +
            $"👤 Cliente: {cliente}\n💈 Servicio: {servicio}\n📅 {fecha}";

        await EnviarACliente(agendamiento, msgCliente, "recordatorio cita");
        await EnviarABarbero(agendamiento, msgBarbero, "recordatorio cita");
    }

    // ─── Helpers ────────────────────────────────────────────────────────────

    private async Task EnviarWhatsAppCancelacionAsync(Agendamiento agendamiento, string motivo)
    {
        if (!_whatsApp.EstaHabilitado) return;

        var cliente = NombreCliente(agendamiento);
        var barbero = NombreBarbero(agendamiento);
        var fecha = FechaHora(agendamiento);

        var msgCliente =
            $"❌ *{AppName}*\n\nHola {cliente}, tu cita fue cancelada ❌\n\n" +
            $"👤 Barbero: {barbero}\n📅 {fecha}\n📝 Motivo: {motivo}";
        var msgBarbero =
            $"❌ *{AppName}*\n\nHola {barbero}, se canceló una cita ❌\n\n" +
            $"👤 Cliente: {cliente}\n📅 {fecha}\n📝 Motivo: {motivo}";
        var msgAdmin =
            $"❌ *{AppName}*\n\nCita cancelada ❌\n\n" +
            $"👤 Cliente: {cliente}\n👤 Barbero: {barbero}\n📅 {fecha}\n📝 Motivo: {motivo}";

        await EnviarACliente(agendamiento, msgCliente, "cancelación cita");
        await EnviarABarbero(agendamiento, msgBarbero, "cancelación cita");
        await EnviarAAdmin(msgAdmin, "cancelación cita");
    }

    private Task EnviarACliente(Agendamiento agendamiento, string mensaje, string contexto)
        => EnviarA(agendamiento.Cliente?.Telefono, mensaje, contexto, "cliente");

    private Task EnviarABarbero(Agendamiento agendamiento, string mensaje, string contexto)
        => EnviarA(agendamiento.Barbero?.Telefono, mensaje, contexto, "barbero");

    private async Task EnviarAAdmin(string mensaje, string contexto)
    {
        // Teléfonos de los administradores activos registrados en Usuarios.
        var telefonosAdmin = await _context.Usuarios
            .Include(u => u.Rol)
            .Where(u => u.Estado && u.Rol != null &&
                (u.Rol.Nombre.ToLower().Contains("admin") || u.Rol.Nombre.ToLower().Contains("administrador")))
            .Where(u => u.Telefono != null && u.Telefono != "")
            .Select(u => u.Telefono!)
            .ToListAsync();

        // Respaldo: número admin del config si ningún administrador tiene teléfono.
        var fallback = _configuration["WhatsApp:AdminTelefono"];
        if (telefonosAdmin.Count == 0 && !string.IsNullOrWhiteSpace(fallback))
            telefonosAdmin.Add(fallback);

        foreach (var tel in telefonosAdmin.Distinct())
            await EnviarA(tel, mensaje, contexto, "admin");
    }

    private async Task EnviarA(string? telefono, string mensaje, string contexto, string destinatario)
    {
        if (string.IsNullOrWhiteSpace(telefono)) return;

        try
        {
            var wa = await _whatsApp.EnviarTextoAsync(telefono, mensaje);
            if (wa.Enviado)
                _logger.LogInformation("WhatsApp {Contexto} enviado a {Destinatario} ({Tel}).", contexto, destinatario, telefono);
            else
                _logger.LogWarning("Fallo WhatsApp {Contexto} a {Destinatario} ({Tel}): {Msg}", contexto, destinatario, telefono, wa.Mensaje);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Excepción WhatsApp {Contexto} a {Destinatario} ({Tel}).", contexto, destinatario, telefono);
        }
    }

    private string AppName => _configuration["Smtp:FromName"] ?? "Manito Barbershop";

    private static string NombreCliente(Agendamiento a)
        => $"{a.Cliente?.Usuario?.Nombre} {a.Cliente?.Usuario?.Apellido}".Trim();

    private static string NombreBarbero(Agendamiento a)
        => $"{a.Barbero?.Usuario?.Nombre} {a.Barbero?.Usuario?.Apellido}".Trim();

    private static string FechaHora(Agendamiento a)
        => a.FechaHora.ToString("dd/MM/yyyy HH:mm");

    private static string Servicio(Agendamiento a)
        => a.Servicio?.Nombre ?? a.Paquete?.Nombre ?? "Servicio";
}
