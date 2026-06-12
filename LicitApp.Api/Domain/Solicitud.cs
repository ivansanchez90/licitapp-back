namespace LicitApp.Api.Domain;

/// <summary>
/// Licitación publicada por un constructor. Contiene materiales (1→N) y recibe ofertas (1→N).
/// </summary>
public class Solicitud
{
    public Guid Id { get; set; }

    // FK -> users.Uid (el dueño).
    public string ConstructorId { get; set; } = default!;

    // Denormalizado: nombre del constructor al momento de crear.
    public string ConstructorName { get; set; } = default!;

    public string Title { get; set; } = default!;
    public string DeliveryZone { get; set; } = default!;
    public DateTime Deadline { get; set; }
    public string? Notes { get; set; }
    public string? AttachmentUrl { get; set; }

    public SolicitudStatus Status { get; set; } = SolicitudStatus.OPEN;

    // La oferta ganadora, una vez aceptada.
    public Guid? WinningOfferId { get; set; }

    // Contadores denormalizados.
    public int OfertasCount { get; set; }
    public int CorralonesNotifiedCount { get; set; }

    public DateTime CreatedAt { get; set; }

    public List<Material> Materiales { get; set; } = new();
    public List<Oferta> Ofertas { get; set; } = new();
}
