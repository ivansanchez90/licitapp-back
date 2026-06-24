using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using LicitApp.Api.Data;
using LicitApp.Api.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace LicitApp.Api.Services;

/// <summary>Opciones del envío de push vía Expo (sección "Notifications:Push").</summary>
public class ExpoPushOptions
{
    /// <summary>Permite apagar el envío (p. ej. en tests o entornos sin tokens reales).</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Endpoint de la Expo Push API. Override sólo para tests/mocks.</summary>
    public string Endpoint { get; set; } = "https://exp.host/--/api/v2/push/send";

    /// <summary>Access token opcional de Expo (para proyectos con "Enhanced Security").</summary>
    public string? AccessToken { get; set; }
}

public interface IPushSender
{
    /// <summary>
    /// Envía las notificaciones dadas vía Expo Push, resolviendo el pushToken de cada
    /// destinatario. Best-effort: nunca lanza; los fallos sólo se loguean.
    /// </summary>
    Task SendAsync(IReadOnlyCollection<Notification> notifications, CancellationToken ct);
}

public class ExpoPushSender : IPushSender
{
    // Expo acepta hasta 100 mensajes por request.
    private const int BatchSize = 100;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private readonly HttpClient _http;
    private readonly AppDbContext _db;
    private readonly ExpoPushOptions _options;
    private readonly ILogger<ExpoPushSender> _logger;

    public ExpoPushSender(
        HttpClient http,
        AppDbContext db,
        IOptions<ExpoPushOptions> options,
        ILogger<ExpoPushSender> logger)
    {
        _http = http;
        _db = db;
        _options = options.Value;
        _logger = logger;
    }

    public async Task SendAsync(IReadOnlyCollection<Notification> notifications, CancellationToken ct)
    {
        if (!_options.Enabled || notifications is null || notifications.Count == 0)
            return;

        try
        {
            // Tokens de los destinatarios (sólo los que tienen pushToken cargado).
            var userIds = notifications.Select(n => n.UserId).Distinct().ToList();
            var tokens = await _db.Users
                .Where(u => userIds.Contains(u.Uid) && u.PushToken != null)
                .Select(u => new { u.Uid, u.PushToken })
                .ToDictionaryAsync(u => u.Uid, u => u.PushToken!, ct);

            var messages = notifications
                .Where(n => tokens.ContainsKey(n.UserId))
                .Select(n => new ExpoMessage(
                    To: tokens[n.UserId],
                    Title: n.Title,
                    Body: n.Body,
                    Sound: "default",
                    Data: new ExpoData(n.Id, n.Type.ToString(), n.SolicitudId, n.OfertaId)))
                .ToList();

            if (messages.Count == 0)
                return;

            foreach (var batch in messages.Chunk(BatchSize))
                await SendBatchAsync(batch, ct);
        }
        catch (Exception ex)
        {
            // El push es best-effort: nunca debe romper la operación de negocio que lo disparó.
            _logger.LogError(ex, "Fallo enviando push vía Expo.");
        }
    }

    private async Task SendBatchAsync(ExpoMessage[] batch, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, _options.Endpoint)
        {
            Content = JsonContent.Create(batch, options: JsonOptions),
        };

        if (!string.IsNullOrWhiteSpace(_options.AccessToken))
            request.Headers.TryAddWithoutValidation("Authorization", $"Bearer {_options.AccessToken}");

        var resp = await _http.SendAsync(request, ct);
        var body = await resp.Content.ReadAsStringAsync(ct);

        if (!resp.IsSuccessStatusCode)
        {
            _logger.LogWarning("Expo Push respondió {Status}: {Body}", (int)resp.StatusCode, body);
            return;
        }

        // La respuesta trae un "ticket" por mensaje; un status "error" indica token inválido, etc.
        if (body.Contains("\"status\":\"error\"", StringComparison.OrdinalIgnoreCase))
            _logger.LogWarning("Expo Push devolvió tickets con error: {Body}", body);
    }

    private sealed record ExpoMessage(string To, string Title, string Body, string Sound, ExpoData Data);

    private sealed record ExpoData(Guid NotificationId, string Type, Guid? SolicitudId, Guid? OfertaId);
}
