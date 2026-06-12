using LicitApp.Api.Auth;
using LicitApp.Api.Dtos;
using LicitApp.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LicitApp.Api.Controllers;

[ApiController]
[Authorize]
public class OfertasController : ControllerBase
{
    private readonly IOfertaService _ofertas;

    public OfertasController(IOfertaService ofertas) => _ofertas = ofertas;

    /// <summary>Crear oferta (rol corralón; reglas de negocio).</summary>
    [HttpPost("api/solicitudes/{id:guid}/ofertas")]
    public async Task<ActionResult<OfertaDto>> Create(Guid id, [FromBody] CreateOfertaRequest req, CancellationToken ct)
    {
        var oferta = await _ofertas.CreateAsync(User.RequireUid(), id, req, ct);
        return CreatedAtAction(nameof(GetOne), new { id, ofertaId = oferta.Id }, oferta.ToDto());
    }

    /// <summary>Ofertas ACTIVE de la solicitud, totalPrice asc.</summary>
    [HttpGet("api/solicitudes/{id:guid}/ofertas")]
    public async Task<ActionResult<List<OfertaDto>>> ForSolicitud(Guid id, CancellationToken ct)
    {
        var list = await _ofertas.GetActiveForSolicitudAsync(id, ct);
        return Ok(list.Select(o => o.ToDto()).ToList());
    }

    /// <summary>Resumen de competencia: { count, bestPrice }.</summary>
    [HttpGet("api/solicitudes/{id:guid}/ofertas/resumen")]
    public async Task<ActionResult<OfertasResumenDto>> Resumen(Guid id, CancellationToken ct)
        => Ok(await _ofertas.GetResumenAsync(id, ct));

    /// <summary>Mis ofertas (corralón), createdAt desc.</summary>
    [HttpGet("api/ofertas/mine")]
    public async Task<ActionResult<List<OfertaDto>>> Mine(CancellationToken ct)
        => Ok(await _ofertas.GetMineAsync(User.RequireUid(), ct));

    /// <summary>Una oferta.</summary>
    [HttpGet("api/solicitudes/{id:guid}/ofertas/{ofertaId:guid}")]
    public async Task<ActionResult<OfertaDto>> GetOne(Guid id, Guid ofertaId, CancellationToken ct)
    {
        var oferta = await _ofertas.GetOneAsync(id, ofertaId, ct);
        return Ok(oferta.ToDto());
    }

    /// <summary>Editar oferta.</summary>
    [HttpPut("api/solicitudes/{id:guid}/ofertas/{ofertaId:guid}")]
    public async Task<ActionResult<OfertaDto>> Update(Guid id, Guid ofertaId, [FromBody] UpdateOfertaRequest req, CancellationToken ct)
    {
        var oferta = await _ofertas.UpdateAsync(User.RequireUid(), id, ofertaId, req, ct);
        return Ok(oferta.ToDto());
    }

    /// <summary>Retirar oferta -> WITHDRAWN.</summary>
    [HttpPost("api/solicitudes/{id:guid}/ofertas/{ofertaId:guid}/withdraw")]
    public async Task<ActionResult<OfertaDto>> Withdraw(Guid id, Guid ofertaId, CancellationToken ct)
    {
        var oferta = await _ofertas.WithdrawAsync(User.RequireUid(), id, ofertaId, ct);
        return Ok(oferta.ToDto());
    }

    /// <summary>Aceptar oferta ganadora (body: ofertaId). Cierra la licitación.</summary>
    [HttpPost("api/solicitudes/{id:guid}/accept")]
    public async Task<ActionResult<SolicitudDto>> Accept(Guid id, [FromBody] AcceptOfertaRequest req, CancellationToken ct)
    {
        var solicitud = await _ofertas.AcceptAsync(User.RequireUid(), id, req.OfertaId, ct);
        return Ok(solicitud.ToDto());
    }
}
