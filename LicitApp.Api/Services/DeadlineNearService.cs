using LicitApp.Api.Data;
using LicitApp.Api.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace LicitApp.Api.Services;

/// <summary>Opciones del job de DEADLINE_NEAR (sección "Notifications:DeadlineNear").</summary>
public class DeadlineNearOptions
{
    /// <summary>Cada cuánto corre el barrido.</summary>
    public int IntervalMinutes { get; set; } = 60;

    /// <summary>Ventana previa al deadline en la que se avisa.</summary>
    public int WindowHours { get; set; } = 24;

    /// <summary>Permite apagar el job (p. ej. en tests).</summary>
    public bool Enabled { get; set; } = true;
}

/// <summary>
/// Job en background: avisa al constructor cuando su licitación OPEN está por cerrar
/// (deadline dentro de la ventana configurada) y todavía no fue notificada.
/// El DEADLINE_NEAR es temporal, por eso vive acá y no en una transacción de evento.
/// </summary>
public class DeadlineNearService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IPushQueue _pushQueue;
    private readonly DeadlineNearOptions _options;
    private readonly ILogger<DeadlineNearService> _logger;

    public DeadlineNearService(
        IServiceScopeFactory scopeFactory,
        IPushQueue pushQueue,
        IOptions<DeadlineNearOptions> options,
        ILogger<DeadlineNearService> logger)
    {
        _scopeFactory = scopeFactory;
        _pushQueue = pushQueue;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation("DeadlineNearService deshabilitado por configuración.");
            return;
        }

        var interval = TimeSpan.FromMinutes(Math.Max(1, _options.IntervalMinutes));
        using var timer = new PeriodicTimer(interval);

        // Corrida inicial al arrancar, y luego en cada tick.
        do
        {
            try
            {
                var created = await SweepAsync(stoppingToken);
                if (created > 0)
                    _logger.LogInformation("DeadlineNear: {Count} notificaciones generadas.", created);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en el barrido de DEADLINE_NEAR.");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    /// <summary>Un barrido. Devuelve cuántas notificaciones se crearon. Idempotente.</summary>
    public async Task<int> SweepAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var now = DateTime.UtcNow;
        var until = now.AddHours(Math.Max(1, _options.WindowHours));

        // Solicitudes OPEN cuyo deadline cae dentro de la ventana y aún no tienen
        // un DEADLINE_NEAR. La subconsulta NOT EXISTS evita duplicados entre corridas.
        var pendientes = await db.Solicitudes
            .Where(s => s.Status == SolicitudStatus.OPEN
                        && s.Deadline > now
                        && s.Deadline <= until
                        && !db.Notifications.Any(n =>
                            n.Type == NotificationType.DEADLINE_NEAR && n.SolicitudId == s.Id))
            .ToListAsync(ct);

        var notifs = pendientes
            .Select(s => NotificationFactory.DeadlineNear(s.ConstructorId, s))
            .ToList();
        db.Notifications.AddRange(notifs);

        if (notifs.Count > 0)
        {
            await db.SaveChangesAsync(ct);
            _pushQueue.Enqueue(notifs);
        }

        return notifs.Count;
    }
}
