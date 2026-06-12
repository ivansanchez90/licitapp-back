using System.ComponentModel.DataAnnotations;
using LicitApp.Api.Domain;

namespace LicitApp.Api.Dtos;

public record MaterialDto(Guid Id, string Name, decimal Quantity, string Unit);

public record SolicitudDto(
    Guid Id,
    string ConstructorId,
    string ConstructorName,
    string Title,
    string DeliveryZone,
    DateTime Deadline,
    string? Notes,
    string? AttachmentUrl,
    SolicitudStatus Status,
    Guid? WinningOfferId,
    int OfertasCount,
    int CorralonesNotifiedCount,
    DateTime CreatedAt,
    List<MaterialDto> Materiales);

public class MaterialInput
{
    [Required, MaxLength(160)]
    public string Name { get; set; } = default!;

    [Range(0.001, double.MaxValue, ErrorMessage = "La cantidad debe ser mayor a 0.")]
    public decimal Quantity { get; set; }

    [Required, MaxLength(40)]
    public string Unit { get; set; } = default!;
}

public class CreateSolicitudRequest
{
    [Required, MaxLength(200)]
    public string Title { get; set; } = default!;

    [Required, MaxLength(120)]
    public string DeliveryZone { get; set; } = default!;

    [Required]
    public DateTime Deadline { get; set; }

    [MaxLength(2000)]
    public string? Notes { get; set; }

    public string? AttachmentUrl { get; set; }

    [Required, MinLength(1, ErrorMessage = "Agregá al menos un material.")]
    public List<MaterialInput> Materiales { get; set; } = new();
}
