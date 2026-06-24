using System.ComponentModel.DataAnnotations;
using LicitApp.Api.Domain;

namespace LicitApp.Api.Dtos;

public record OfertaDto(
    Guid Id,
    Guid SolicitudId,
    string CorralonId,
    string CorralonName,
    double? CorralonRating,
    decimal TotalPrice,
    ShippingType ShippingType,
    decimal? ShippingPrice,
    int DeliveryHours,
    DateTime ValidUntil,
    string? Comment,
    OfertaStatus Status,
    bool IsBestPrice,
    bool IsFastDelivery,
    DateTime CreatedAt,
    // URL pública del presupuesto/foto adjunta. Puede venir null.
    string? AttachmentUrl,
    // Denormalizados para listas del corralón (join con la Solicitud).
    string? SolicitudTitle,
    DateTime? SolicitudDeadline);

/// <summary>Resumen de competencia: { count, bestPrice }.</summary>
public record OfertasResumenDto(int Count, decimal? BestPrice);

public class CreateOfertaRequest
{
    [Range(0.01, double.MaxValue, ErrorMessage = "El precio total debe ser mayor a 0.")]
    public decimal TotalPrice { get; set; }

    [Required]
    public ShippingType ShippingType { get; set; }

    public decimal? ShippingPrice { get; set; }

    [Range(0, int.MaxValue)]
    public int DeliveryHours { get; set; }

    [Required]
    public DateTime ValidUntil { get; set; }

    [MaxLength(1000)]
    public string? Comment { get; set; }

    /// <summary>
    /// URL pública del presupuesto/foto (Firebase Storage u otro https). Opcional.
    /// Vacío se trata como null. Debe ser una URL absoluta válida (validado en el servicio).
    /// Ejemplo: https://firebasestorage.googleapis.com/v0/b/licitapp-e1841.firebasestorage.app/o/attachments%2F&lt;uid&gt;%2F1715000000_presupuesto.pdf?alt=media&amp;token=...
    /// </summary>
    [MaxLength(2048)]
    public string? AttachmentUrl { get; set; }
}

public class UpdateOfertaRequest
{
    [Range(0.01, double.MaxValue, ErrorMessage = "El precio total debe ser mayor a 0.")]
    public decimal TotalPrice { get; set; }

    [Required]
    public ShippingType ShippingType { get; set; }

    public decimal? ShippingPrice { get; set; }

    [Range(0, int.MaxValue)]
    public int DeliveryHours { get; set; }

    [MaxLength(1000)]
    public string? Comment { get; set; }

    /// <summary>
    /// URL pública del presupuesto/foto. Opcional. Pasar null (o vacío) borra el adjunto.
    /// Si viene un valor, debe ser una URL absoluta válida (validado en el servicio).
    /// </summary>
    [MaxLength(2048)]
    public string? AttachmentUrl { get; set; }
}

public class AcceptOfertaRequest
{
    [Required]
    public Guid OfertaId { get; set; }
}
