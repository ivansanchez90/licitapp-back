using System.Threading.Channels;
using LicitApp.Api.Domain;

namespace LicitApp.Api.Services;

public interface IPushQueue
{
    /// <summary>
    /// Encola notificaciones para enviar push de forma asíncrona. No bloquea ni lanza:
    /// el envío real lo procesa <see cref="PushQueueProcessor"/> en segundo plano.
    /// </summary>
    void Enqueue(IReadOnlyCollection<Notification> notifications);
}

/// <summary>
/// Cola en memoria (Channel ilimitado, un solo lector) entre los servicios de negocio
/// y el envío de push. Singleton: la misma instancia para productores y consumidor.
/// </summary>
public class PushQueue : IPushQueue
{
    private readonly Channel<IReadOnlyCollection<Notification>> _channel =
        Channel.CreateUnbounded<IReadOnlyCollection<Notification>>(new UnboundedChannelOptions
        {
            SingleReader = true,
        });

    public ChannelReader<IReadOnlyCollection<Notification>> Reader => _channel.Reader;

    public void Enqueue(IReadOnlyCollection<Notification> notifications)
    {
        if (notifications is null || notifications.Count == 0)
            return;

        // Las Notification ya están materializadas (POCO sin navegaciones), así que es
        // seguro pasarlas a otro hilo aunque el DbContext que las creó se haya liberado.
        // Channel ilimitado y sin completar => TryWrite nunca falla.
        _channel.Writer.TryWrite(notifications);
    }
}

/// <summary>
/// Consume la <see cref="PushQueue"/> y envía cada lote vía <see cref="IPushSender"/>
/// (Expo) en un scope propio. Aísla la latencia/errores del push del request HTTP.
/// </summary>
public class PushQueueProcessor : BackgroundService
{
    private readonly PushQueue _queue;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<PushQueueProcessor> _logger;

    public PushQueueProcessor(
        PushQueue queue,
        IServiceScopeFactory scopeFactory,
        ILogger<PushQueueProcessor> logger)
    {
        _queue = queue;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var batch in _queue.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                // Scope propio: IPushSender es scoped (usa AppDbContext + HttpClient).
                using var scope = _scopeFactory.CreateScope();
                var sender = scope.ServiceProvider.GetRequiredService<IPushSender>();
                await sender.SendAsync(batch, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                // El procesador no debe morir por un lote fallido.
                _logger.LogError(ex, "Error procesando lote de push encolado.");
            }
        }
    }
}
