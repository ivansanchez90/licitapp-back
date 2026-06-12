using LicitApp.Api.Domain;

namespace LicitApp.Api.Dtos;

/// <summary>Mapeos entidad -> DTO. Mantiene las entidades EF fuera de la API pública.</summary>
public static class Mapping
{
    public static StatsDto ToDto(this UserStats s)
        => new(s.TotalLicitaciones, s.TotalOfertas, s.TotalCierres);

    public static UserDto ToDto(this User u)
        => new(u.Uid, u.Email, u.FullName, u.Role, u.Phone, u.Zone, u.BusinessName,
               u.Verified, u.PushToken, u.CreatedAt, u.Stats.ToDto());

    public static UserContactDto ToContactDto(this User u)
        => new(u.Uid, u.FullName, u.Role, u.Phone, u.Zone, u.BusinessName, u.Verified);

    public static MaterialDto ToDto(this Material m)
        => new(m.Id, m.Name, m.Quantity, m.Unit);

    public static SolicitudDto ToDto(this Solicitud s)
        => new(s.Id, s.ConstructorId, s.ConstructorName, s.Title, s.DeliveryZone,
               s.Deadline, s.Notes, s.AttachmentUrl, s.Status, s.WinningOfferId,
               s.OfertasCount, s.CorralonesNotifiedCount, s.CreatedAt,
               s.Materiales.Select(m => m.ToDto()).ToList());

    public static OfertaDto ToDto(this Oferta o, string? solicitudTitle = null, DateTime? solicitudDeadline = null)
        => new(o.Id, o.SolicitudId, o.CorralonId, o.CorralonName, o.CorralonRating,
               o.TotalPrice, o.ShippingType, o.ShippingPrice, o.DeliveryHours,
               o.ValidUntil, o.Comment, o.Status, o.IsBestPrice, o.IsFastDelivery,
               o.CreatedAt, solicitudTitle, solicitudDeadline);

    public static NotificationDto ToDto(this Notification n)
        => new(n.Id, n.UserId, n.Type, n.Title, n.Body, n.SolicitudId, n.OfertaId, n.Read, n.CreatedAt);
}
