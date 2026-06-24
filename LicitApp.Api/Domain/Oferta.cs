namespace LicitApp.Api.Domain;

/// <summary>
/// Cotización publicada por un corralón sobre una solicitud.
/// Los campos denormalizados que el front consume en listas (solicitudTitle,
/// solicitudDeadline) NO se persisten: se exponen en el DTO vía join con la Solicitud.
/// </summary>
public class Oferta
{
    public Guid Id { get; set; }

    // FK -> solicitudes.Id.
    public Guid SolicitudId { get; set; }
    public Solicitud? Solicitud { get; set; }

    // FK -> users.Uid (el corralón).
    public string CorralonId { get; set; } = default!;

    // Denormalizado: nombre del corralón.
    public string CorralonName { get; set; } = default!;
    public double? CorralonRating { get; set; }

    public decimal TotalPrice { get; set; }
    public ShippingType ShippingType { get; set; }

    // Sólo si ShippingType es CHARGED / FIXED_PRICE.
    public decimal? ShippingPrice { get; set; }

    public int DeliveryHours { get; set; }
    public DateTime ValidUntil { get; set; }
    public string? Comment { get; set; }

    // URL pública del presupuesto/foto adjunta (Firebase Storage u otro). El backend
    // sólo persiste la URL: no sube ni almacena bytes. Mismo patrón que Solicitud.AttachmentUrl.
    public string? AttachmentUrl { get; set; }

    public OfertaStatus Status { get; set; } = OfertaStatus.ACTIVE;

    // Badge: es la oferta ACTIVE más barata.
    public bool IsBestPrice { get; set; }

    // Badge: entrega <= 24 h.
    public bool IsFastDelivery { get; set; }

    public DateTime CreatedAt { get; set; }
}
