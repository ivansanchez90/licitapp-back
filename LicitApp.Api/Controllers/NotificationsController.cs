using LicitApp.Api.Auth;
using LicitApp.Api.Dtos;
using LicitApp.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LicitApp.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/notifications")]
public class NotificationsController : ControllerBase
{
    private readonly INotificationService _notifications;

    public NotificationsController(INotificationService notifications) => _notifications = notifications;

    /// <summary>Notificaciones del usuario autenticado.</summary>
    [HttpGet]
    public async Task<ActionResult<List<NotificationDto>>> Mine(CancellationToken ct)
    {
        var list = await _notifications.GetMineAsync(User.RequireUid(), ct);
        return Ok(list.Select(n => n.ToDto()).ToList());
    }

    /// <summary>Marcar una notificación como leída.</summary>
    [HttpPost("{id:guid}/read")]
    public async Task<IActionResult> MarkRead(Guid id, CancellationToken ct)
    {
        await _notifications.MarkReadAsync(User.RequireUid(), id, ct);
        return NoContent();
    }
}
