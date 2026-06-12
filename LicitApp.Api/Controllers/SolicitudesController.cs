using LicitApp.Api.Auth;
using LicitApp.Api.Domain;
using LicitApp.Api.Dtos;
using LicitApp.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LicitApp.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/solicitudes")]
public class SolicitudesController : ControllerBase
{
    private readonly ISolicitudService _solicitudes;

    public SolicitudesController(ISolicitudService solicitudes) => _solicitudes = solicitudes;

    /// <summary>Crear solicitud + materiales (rol constructor).</summary>
    [HttpPost]
    public async Task<ActionResult<SolicitudDto>> Create([FromBody] CreateSolicitudRequest req, CancellationToken ct)
    {
        var solicitud = await _solicitudes.CreateAsync(User.RequireUid(), req, ct);
        return CreatedAtAction(nameof(GetById), new { id = solicitud.Id }, solicitud.ToDto());
    }

    /// <summary>Mis solicitudes (constructor), orden createdAt desc.</summary>
    [HttpGet("mine")]
    public async Task<ActionResult<List<SolicitudDto>>> Mine(CancellationToken ct)
    {
        var list = await _solicitudes.GetMineAsync(User.RequireUid(), ct);
        return Ok(list.Select(s => s.ToDto()).ToList());
    }

    /// <summary>Detalle con materiales.</summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<SolicitudDto>> GetById(Guid id, CancellationToken ct)
    {
        var solicitud = await _solicitudes.GetByIdAsync(id, ct);
        return Ok(solicitud.ToDto());
    }

    /// <summary>Feed del corralón por zona, orden deadline asc.</summary>
    [HttpGet]
    public async Task<ActionResult<List<SolicitudDto>>> Feed(
        [FromQuery] string? zone,
        [FromQuery] SolicitudStatus? status,
        CancellationToken ct)
    {
        var list = await _solicitudes.GetFeedAsync(zone, status, ct);
        return Ok(list.Select(s => s.ToDto()).ToList());
    }
}
