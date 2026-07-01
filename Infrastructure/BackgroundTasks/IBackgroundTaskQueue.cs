namespace BarberiaApi.Infrastructure.BackgroundTasks;

/// <summary>
/// Cola de tareas en segundo plano. Permite encolar trabajo (ej. envío de correos/WhatsApp)
/// para que la petición HTTP responda de inmediato y el trabajo se procese aparte.
/// Cada tarea recibe un IServiceProvider con su propio scope de DI.
/// </summary>
public interface IBackgroundTaskQueue
{
    void Enqueue(Func<IServiceProvider, CancellationToken, Task> workItem);

    ValueTask<Func<IServiceProvider, CancellationToken, Task>> DequeueAsync(CancellationToken cancellationToken);
}
