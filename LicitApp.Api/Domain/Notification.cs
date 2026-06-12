namespace LicitApp.Api.Domain;

/// <summary>
/// Notificación persistida para un usuario. El envío vía Expo Push queda fuera de
/// alcance en esta iteración; acá sólo se persiste.
/// </summary>
public class Notification
{
    public Guid Id { get; set; }

    // FK -> users.Uid (destinatario).
    public string UserId { get; set; } = default!;

    public NotificationType Type { get; set; }
    public string Title { get; set; } = default!;
    public string Body { get; set; } = default!;

    public Guid? SolicitudId { get; set; }
    public Guid? OfertaId { get; set; }

    public bool Read { get; set; }
    public DateTime CreatedAt { get; set; }
}
