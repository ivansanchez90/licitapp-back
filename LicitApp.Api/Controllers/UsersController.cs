using LicitApp.Api.Auth;
using LicitApp.Api.Dtos;
using LicitApp.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LicitApp.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/users")]
public class UsersController : ControllerBase
{
    private readonly IUserService _users;

    public UsersController(IUserService users) => _users = users;

    /// <summary>Upsert del perfil del usuario autenticado (registro/login).</summary>
    [HttpPost("sync")]
    public async Task<ActionResult<UserDto>> Sync([FromBody] SyncUserRequest req, CancellationToken ct)
    {
        var uid = User.RequireUid();
        var user = await _users.SyncAsync(uid, User.GetEmail(), req, ct);
        return Ok(user.ToDto());
    }

    /// <summary>Perfil del usuario autenticado (incluye stats).</summary>
    [HttpGet("me")]
    public async Task<ActionResult<UserDto>> Me(CancellationToken ct)
    {
        var user = await _users.GetRequiredAsync(User.RequireUid(), ct);
        return Ok(user.ToDto());
    }

    /// <summary>Actualizar perfil (incluye pushToken).</summary>
    [HttpPut("me")]
    public async Task<ActionResult<UserDto>> UpdateMe([FromBody] UpdateUserRequest req, CancellationToken ct)
    {
        var user = await _users.UpdateAsync(User.RequireUid(), req, ct);
        return Ok(user.ToDto());
    }

    /// <summary>
    /// Datos de contacto de una contraparte. Sólo accesible si el usuario autenticado y
    /// {uid} comparten una operación cerrada (constructor ↔ corralón ganador).
    /// Responde 404 si no existe esa relación (privacidad) o si el usuario no existe.
    /// </summary>
    [HttpGet("{uid}")]
    public async Task<ActionResult<UserContactDto>> GetContact(string uid, CancellationToken ct)
    {
        var user = await _users.GetContactForCounterpartyAsync(User.RequireUid(), uid, ct);
        return user is null ? NotFound() : Ok(user.ToContactDto());
    }
}
