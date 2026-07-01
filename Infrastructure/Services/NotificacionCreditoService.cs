using System.Net;
using BarberiaApi.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace BarberiaApi.Infrastructure.Services;

public sealed class NotificacionCreditoService : INotificacionCreditoService
{
    private readonly BarberiaContext _context;
    private readonly IEmailProxyService _email;
    private readonly IWhatsAppService _whatsApp;
    private readonly ILogger<NotificacionCreditoService> _logger;
    private readonly IConfiguration _configuration;

    public NotificacionCreditoService(
        BarberiaContext context,
        IEmailProxyService email,
        IWhatsAppService whatsApp,
        ILogger<NotificacionCreditoService> logger,
        IConfiguration configuration)
    {
        _context = context;
        _email = email;
        _whatsApp = whatsApp;
        _logger = logger;
        _configuration = configuration;
    }

    public async Task NotificarCreditoBloqueadoAsync(
        int barberoId,
        string barberoNombre,
        string correo,
        decimal saldoDeuda,
        decimal cupoMaximo,
        string? telefono = null,
        CancellationToken cancellationToken = default)
    {
        var appName = _configuration["AppName"] ?? "Barbería App";

        if (!string.IsNullOrWhiteSpace(correo))
        {
            var subject = $"{appName} - Tu crédito ha sido bloqueado";
            var html = BuildBloqueoHtml(barberoNombre, saldoDeuda, cupoMaximo, appName);
            var result = await _email.EnviarEmailAsync(correo, barberoNombre, subject, html, cancellationToken);
            if (result.Enviado)
                _logger.LogInformation("Email crédito bloqueado enviado a {Email} (barberoId={Id}).", correo, barberoId);
            else
                _logger.LogWarning("Fallo email crédito bloqueado a {Email}: {Msg}", correo, result.Mensaje);
        }
        else
        {
            _logger.LogWarning("No se pudo enviar email crédito bloqueado para barberoId={Id}: correo vacío.", barberoId);
        }

        if (_whatsApp.EstaHabilitado && !string.IsNullOrWhiteSpace(telefono))
        {
            var mensaje =
                $"🔴 *{appName}*\n\nHola {barberoNombre}, tu crédito fue bloqueado.\n\n" +
                $"💰 Deuda: {saldoDeuda:C0}\n📊 Cupo máximo: {cupoMaximo:C0}\n\n" +
                $"Realiza un abono para desbloquearlo. Comunícate con el administrador.";
            var wa = await _whatsApp.EnviarTextoAsync(telefono, mensaje, cancellationToken);
            if (wa.Enviado)
                _logger.LogInformation("WhatsApp crédito bloqueado enviado a {Tel} (barberoId={Id}).", telefono, barberoId);
            else
                _logger.LogWarning("Fallo WhatsApp crédito bloqueado a {Tel}: {Msg}", telefono, wa.Mensaje);
        }
    }

    public async Task NotificarBloqueadosActivosAsync(CancellationToken cancellationToken = default)
    {
        var bloqueados = await _context.CreditosBarbero
            .Include(c => c.Barbero).ThenInclude(b => b.Usuario)
            .AsNoTracking()
            .Where(c => c.Estado == "BloqueadoLimite" || c.Estado == "BloqueadoVencimiento" || c.Estado == "BloqueadoLimiteYVencimiento")
            .Where(c => c.SaldoPendiente > 0)
            .ToListAsync(cancellationToken);

        foreach (var credito in bloqueados)
        {
            if (cancellationToken.IsCancellationRequested) break;

            var correo = credito.Barbero?.Usuario?.Correo;
            var nombre = credito.Barbero?.Usuario != null
                ? $"{credito.Barbero.Usuario.Nombre} {credito.Barbero.Usuario.Apellido}".Trim()
                : "Barbero";

            await NotificarCreditoBloqueadoAsync(
                credito.BarberoId, nombre, correo ?? string.Empty,
                credito.SaldoPendiente, credito.LimiteCredito,
                telefono: credito.Barbero?.Telefono,
                cancellationToken: cancellationToken);
        }

        _logger.LogInformation("Notificaciones periódicas enviadas a {Count} barberos bloqueados.", bloqueados.Count);
    }

    public async Task EnviarResumenSemanalAdminAsync(CancellationToken cancellationToken = default)
    {
        var admins = await _context.Usuarios
            .Include(u => u.Rol)
            .AsNoTracking()
            .Where(u => u.Rol != null &&
                        (u.Rol.Nombre == "admin" || u.Rol.Nombre == "super_admin" ||
                         u.Rol.Nombre == "Admin" || u.Rol.Nombre == "SuperAdmin"))
            .ToListAsync(cancellationToken);

        if (admins.Count == 0)
        {
            _logger.LogWarning("Resumen semanal: no se encontraron administradores activos.");
            return;
        }

        var bloqueados = await _context.CreditosBarbero
            .Include(c => c.Barbero).ThenInclude(b => b.Usuario)
            .AsNoTracking()
            .Where(c => c.Estado == "BloqueadoLimite" || c.Estado == "BloqueadoVencimiento" || c.Estado == "BloqueadoLimiteYVencimiento")
            .ToListAsync(cancellationToken);

        var activos = await _context.CreditosBarbero
            .AsNoTracking()
            .CountAsync(c => c.Estado == "Activo", cancellationToken);

        var deudaTotal = await _context.CreditosBarbero
            .AsNoTracking()
            .SumAsync(c => c.SaldoPendiente, cancellationToken);

        var appName = _configuration["AppName"] ?? "Barbería App";
        var subject = $"{appName} - Resumen semanal de créditos de barberos";
        var html = BuildResumenHtml(bloqueados.Count, activos, deudaTotal, bloqueados.Select(c =>
            new BloqueadoResumen
            {
                Nombre = c.Barbero?.Usuario != null
                    ? $"{c.Barbero.Usuario.Nombre} {c.Barbero.Usuario.Apellido}".Trim()
                    : "Barbero",
                SaldoDeuda = c.SaldoPendiente,
                CupoMaximo = c.LimiteCredito
            }).ToList(), appName);

        foreach (var admin in admins)
        {
            if (cancellationToken.IsCancellationRequested) break;
            if (string.IsNullOrWhiteSpace(admin.Correo)) continue;

            var nombre = $"{admin.Nombre} {admin.Apellido}".Trim();
            var result = await _email.EnviarEmailAsync(admin.Correo, nombre, subject, html, cancellationToken);

            if (result.Enviado)
                _logger.LogInformation("Resumen semanal enviado a admin {Email}.", admin.Correo);
            else
                _logger.LogWarning("Fallo al enviar resumen semanal a {Email}: {Msg}", admin.Correo, result.Mensaje);
        }

        if (_whatsApp.EstaHabilitado)
        {
            var adminTel = _configuration["WhatsApp:AdminTelefono"];
            if (!string.IsNullOrWhiteSpace(adminTel))
            {
                var mensaje =
                    $"📊 *{appName}*\n\nResumen semanal de créditos:\n\n" +
                    $"🔴 Bloqueados: {bloqueados.Count}\n🟢 Activos: {activos}\n💰 Deuda total: {deudaTotal:C0}";
                var wa = await _whatsApp.EnviarTextoAsync(adminTel, mensaje, cancellationToken);
                if (wa.Enviado)
                    _logger.LogInformation("WhatsApp resumen semanal enviado a admin {Tel}.", adminTel);
                else
                    _logger.LogWarning("Fallo WhatsApp resumen semanal a {Tel}: {Msg}", adminTel, wa.Mensaje);
            }
        }
    }

    private static string BuildBloqueoHtml(string nombre, decimal saldoDeuda, decimal cupoMaximo, string appName)
    {
        var safeNombre = WebUtility.HtmlEncode(nombre);
        var safeApp = WebUtility.HtmlEncode(appName);
        var safeSaldo = WebUtility.HtmlEncode(saldoDeuda.ToString("C0"));
        var safeCupo = WebUtility.HtmlEncode(cupoMaximo.ToString("C0"));
        var porcentaje = cupoMaximo > 0 ? (int)Math.Round(saldoDeuda / cupoMaximo * 100) : 100;

        return $@"<div style=""font-family:'Segoe UI',Tahoma,Geneva,Verdana,sans-serif;max-width:600px;margin:0 auto;background-color:#0a0a0a;color:#ffffff;border:1px solid #333;border-radius:16px;overflow:hidden;"">
  <div style=""height:6px;background-color:#DC2626;""></div>
  <div style=""background-color:#111;padding:36px 20px;text-align:center;"">
    <div style=""display:inline-block;padding:8px 18px;border:1px solid #d8b081;border-radius:4px;margin-bottom:16px;"">
      <span style=""font-size:22px;font-weight:900;letter-spacing:4px;color:#ffffff;text-transform:uppercase;"">MANITO</span>
      <span style=""font-size:22px;font-weight:300;letter-spacing:4px;color:#d8b081;text-transform:uppercase;"">BARBERSHOP</span>
    </div>
    <h1 style=""color:#DC2626;margin:8px 0 0;font-size:26px;font-weight:800;text-transform:uppercase;"">Crédito Bloqueado</h1>
  </div>
  <div style=""padding:36px 30px;background-color:#0a0a0a;"">
    <p style=""font-size:17px;color:#ffffff;margin-bottom:20px;"">Hola <strong>{safeNombre}</strong>,</p>
    <p style=""color:#aaa;line-height:1.8;font-size:15px;margin-bottom:28px;"">
      Te informamos que tu crédito en <strong style=""color:#d8b081;"">{safeApp}</strong> ha sido <strong style=""color:#DC2626;"">bloqueado</strong>
      porque has alcanzado o superado el cupo máximo asignado.
    </p>
    <div style=""background-color:#1a1a1a;border-left:4px solid #DC2626;padding:22px;border-radius:8px;margin-bottom:28px;"">
      <table style=""width:100%;border-collapse:collapse;"">
        <tr>
          <td style=""padding:10px 0;border-bottom:1px solid #222;color:#888;font-size:14px;width:50%;"">Saldo de deuda:</td>
          <td style=""padding:10px 0;border-bottom:1px solid #222;color:#DC2626;font-size:16px;font-weight:700;"">{safeSaldo}</td>
        </tr>
        <tr>
          <td style=""padding:10px 0;color:#888;font-size:14px;"">Cupo máximo:</td>
          <td style=""padding:10px 0;color:#fff;font-size:16px;font-weight:700;"">{safeCupo}</td>
        </tr>
      </table>
      <div style=""margin-top:16px;"">
        <div style=""background-color:#333;border-radius:6px;height:10px;overflow:hidden;"">
          <div style=""background-color:#DC2626;height:100%;width:{Math.Min(porcentaje, 100)}%;border-radius:6px;""></div>
        </div>
        <p style=""color:#888;font-size:12px;margin-top:6px;text-align:right;"">{porcentaje}% del cupo utilizado</p>
      </div>
    </div>
    <p style=""color:#aaa;font-size:14px;line-height:1.7;"">
      Para desbloquear tu crédito, debes realizar un abono que cubra parte o la totalidad de tu deuda.
      Comunícate con el administrador para coordinar el pago.
    </p>
  </div>
  <div style=""background-color:#000;padding:32px 20px;text-align:center;border-top:1px solid #222;"">
    <p style=""color:#555;font-size:12px;margin:0;"">© 2026 <strong style=""color:#777;"">{safeApp}</strong>. Este es un correo automático.</p>
  </div>
</div>";
    }

    private static string BuildResumenHtml(int bloqueados, int activos, decimal deudaTotal,
        List<BloqueadoResumen> detalle, string appName)
    {
        var safeApp = WebUtility.HtmlEncode(appName);
        var filas = detalle.Count == 0
            ? "<tr><td colspan=\"3\" style=\"padding:12px;text-align:center;color:#888;\">Sin barberos bloqueados</td></tr>"
            : string.Join("", detalle.Select(d =>
                $"<tr><td style=\"padding:10px 8px;border-bottom:1px solid #222;color:#fff;\">{WebUtility.HtmlEncode(d.Nombre)}</td>" +
                $"<td style=\"padding:10px 8px;border-bottom:1px solid #222;color:#DC2626;font-weight:700;\">{d.SaldoDeuda:C0}</td>" +
                $"<td style=\"padding:10px 8px;border-bottom:1px solid #222;color:#888;\">{d.CupoMaximo:C0}</td></tr>"));

        return $@"<div style=""font-family:'Segoe UI',Tahoma,Geneva,Verdana,sans-serif;max-width:600px;margin:0 auto;background-color:#0a0a0a;color:#ffffff;border:1px solid #333;border-radius:16px;overflow:hidden;"">
  <div style=""height:6px;background-color:#d8b081;""></div>
  <div style=""background-color:#111;padding:36px 20px;text-align:center;"">
    <div style=""display:inline-block;padding:8px 18px;border:1px solid #d8b081;border-radius:4px;margin-bottom:16px;"">
      <span style=""font-size:22px;font-weight:900;letter-spacing:4px;color:#ffffff;text-transform:uppercase;"">MANITO</span>
      <span style=""font-size:22px;font-weight:300;letter-spacing:4px;color:#d8b081;text-transform:uppercase;"">BARBERSHOP</span>
    </div>
    <h1 style=""color:#d8b081;margin:8px 0 0;font-size:24px;font-weight:800;text-transform:uppercase;"">Resumen Semanal — Créditos</h1>
  </div>
  <div style=""padding:36px 30px;background-color:#0a0a0a;"">
    <div style=""display:flex;gap:12px;margin-bottom:30px;"">
      <div style=""flex:1;background-color:#1a1a1a;border-radius:8px;padding:18px;text-align:center;border:1px solid #222;"">
        <p style=""color:#888;font-size:12px;text-transform:uppercase;letter-spacing:1px;margin:0 0 8px;"">Bloqueados</p>
        <p style=""color:#DC2626;font-size:28px;font-weight:800;margin:0;"">{bloqueados}</p>
      </div>
      <div style=""flex:1;background-color:#1a1a1a;border-radius:8px;padding:18px;text-align:center;border:1px solid #222;"">
        <p style=""color:#888;font-size:12px;text-transform:uppercase;letter-spacing:1px;margin:0 0 8px;"">Activos</p>
        <p style=""color:#7aab8a;font-size:28px;font-weight:800;margin:0;"">{activos}</p>
      </div>
      <div style=""flex:1;background-color:#1a1a1a;border-radius:8px;padding:18px;text-align:center;border:1px solid #222;"">
        <p style=""color:#888;font-size:12px;text-transform:uppercase;letter-spacing:1px;margin:0 0 8px;"">Deuda total</p>
        <p style=""color:#d8b081;font-size:22px;font-weight:800;margin:0;"">{deudaTotal:C0}</p>
      </div>
    </div>
    <h3 style=""color:#d8b081;font-size:13px;text-transform:uppercase;letter-spacing:2px;margin-bottom:14px;"">Barberos bloqueados</h3>
    <table style=""width:100%;border-collapse:collapse;"">
      <thead>
        <tr style=""background-color:#1a1a1a;"">
          <th style=""padding:10px 8px;text-align:left;color:#888;font-size:12px;text-transform:uppercase;letter-spacing:1px;"">Barbero</th>
          <th style=""padding:10px 8px;text-align:left;color:#888;font-size:12px;text-transform:uppercase;letter-spacing:1px;"">Deuda</th>
          <th style=""padding:10px 8px;text-align:left;color:#888;font-size:12px;text-transform:uppercase;letter-spacing:1px;"">Cupo</th>
        </tr>
      </thead>
      <tbody>{filas}</tbody>
    </table>
  </div>
  <div style=""background-color:#000;padding:32px 20px;text-align:center;border-top:1px solid #222;"">
    <p style=""color:#555;font-size:12px;margin:0;"">© 2026 <strong style=""color:#777;"">{safeApp}</strong>. Resumen generado automáticamente.</p>
  </div>
</div>";
    }

    private sealed record BloqueadoResumen
    {
        public string Nombre { get; init; } = string.Empty;
        public decimal SaldoDeuda { get; init; }
        public decimal CupoMaximo { get; init; }
    }
}
