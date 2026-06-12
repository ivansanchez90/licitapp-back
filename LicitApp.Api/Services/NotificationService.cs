using LicitApp.Api.Data;
using LicitApp.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace LicitApp.Api.Services;

public interface INotificationService
{
    Task<List<Notification>> GetMineAsync(string uid, CancellationToken ct);
    Task MarkReadAsync(string uid, Guid id, CancellationToken ct);
}

public class NotificationService : INotificationService
{
    private readonly AppDbContext _db;

    public NotificationService(AppDbContext db) => _db = db;

    public Task<List<Notification>> GetMineAsync(string uid, CancellationToken ct)
        => _db.Notifications
            .Where(n => n.UserId == uid)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync(ct);

    public async Task MarkReadAsync(string uid, Guid id, CancellationToken ct)
    {
        var n = await _db.Notifications.FirstOrDefaultAsync(x => x.Id == id && x.UserId == uid, ct)
            ?? throw AppException.NotFound("Notificación no encontrada.");
        n.Read = true;
        await _db.SaveChangesAsync(ct);
    }
}
