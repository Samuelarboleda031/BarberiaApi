namespace BarberiaApi.Infrastructure.Services;

public interface INotificacionCreditoService
{
    Task NotificarCreditoBloqueadoAsync(
        int barberoId,
        string barberoNombre,
        string correo,
        decimal saldoDeuda,
        decimal cupoMaximo,
        CancellationToken cancellationToken = default);

    Task NotificarBloqueadosActivosAsync(CancellationToken cancellationToken = default);

    Task EnviarResumenSemanalAdminAsync(CancellationToken cancellationToken = default);
}
