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
}

public class AcceptOfertaRequest
{
    [Required]
    public Guid OfertaId { get; set; }
}
