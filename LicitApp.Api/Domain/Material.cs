namespace LicitApp.Api.Domain;

/// <summary>
/// Ítem de material pedido en una solicitud. Cascade delete con la solicitud.
/// </summary>
public class Material
{
    public Guid Id { get; set; }

    // FK -> solicitudes.Id (cascade delete).
    public Guid SolicitudId { get; set; }
    public Solicitud? Solicitud { get; set; }

    public string Name { get; set; } = default!;
    public decimal Quantity { get; set; }

    // Una de las MATERIAL_UNITS (bolsas, unidades, m², etc.).
    public string Unit { get; set; } = default!;
}
