using LicitApp.Api.Domain;

namespace LicitApp.Api.Services;

/// <summary>
/// Construye las entidades Notification con textos consistentes. El envío real (Expo Push)
/// queda fuera de alcance; acá sólo se persisten dentro de la transacción del evento.
/// </summary>
public static class NotificationFactory
{
    public static Notification NewRequest(string corralonUid, Solicitud s) => new()
    {
        Id = Guid.NewGuid(),
        UserId = corralonUid,
        Type = NotificationType.NEW_REQUEST,
        Title = "Nueva licitación en tu zona",
        Body = $"{s.ConstructorName} publicó \"{s.Title}\" en {s.DeliveryZone}.",
        SolicitudId = s.Id,
        Read = false,
        CreatedAt = DateTime.UtcNow,
    };

    public static Notification DeadlineNear(string constructorUid, Solicitud s) => new()
    {
        Id = Guid.NewGuid(),
        UserId = constructorUid,
        Type = NotificationType.DEADLINE_NEAR,
        Title = "Tu licitación está por cerrar",
        Body = $"\"{s.Title}\" cierra el {s.Deadline:dd/MM HH:mm} UTC. Revisá las ofertas recibidas.",
        SolicitudId = s.Id,
        Read = false,
        CreatedAt = DateTime.UtcNow,
    };

    public static Notification NewOffer(string constructorUid, Solicitud s, Oferta o) => new()
    {
        Id = Guid.NewGuid(),
        UserId = constructorUid,
        Type = NotificationType.NEW_OFFER,
        Title = "Nueva oferta recibida",
        Body = $"{o.CorralonName} ofertó ${o.TotalPrice:0.##} en \"{s.Title}\".",
        SolicitudId = s.Id,
        OfertaId = o.Id,
        Read = false,
        CreatedAt = DateTime.UtcNow,
    };

    public static Notification OfferWon(Solicitud s, Oferta o) => new()
    {
        Id = Guid.NewGuid(),
        UserId = o.CorralonId,
        Type = NotificationType.OFFER_WON,
        Title = "¡Ganaste la licitación!",
        Body = $"Tu oferta en \"{s.Title}\" fue aceptada.",
        SolicitudId = s.Id,
        OfertaId = o.Id,
        Read = false,
        CreatedAt = DateTime.UtcNow,
    };

    public static Notification OfferLost(Solicitud s, Oferta o) => new()
    {
        Id = Guid.NewGuid(),
        UserId = o.CorralonId,
        Type = NotificationType.OFFER_LOST,
        Title = "Licitación cerrada",
        Body = $"Tu oferta en \"{s.Title}\" no fue seleccionada.",
        SolicitudId = s.Id,
        OfertaId = o.Id,
        Read = false,
        CreatedAt = DateTime.UtcNow,
    };
}
