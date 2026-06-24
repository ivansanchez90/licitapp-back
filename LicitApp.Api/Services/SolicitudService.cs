using LicitApp.Api.Data;
using LicitApp.Api.Domain;
using LicitApp.Api.Dtos;
using Microsoft.EntityFrameworkCore;

namespace LicitApp.Api.Services;

public interface ISolicitudService
{
    Task<Solicitud> CreateAsync(string uid, CreateSolicitudRequest req, CancellationToken ct);
    Task<List<Solicitud>> GetMineAsync(string uid, CancellationToken ct);
    Task<Solicitud> GetByIdAsync(Guid id, CancellationToken ct);
    Task<List<Solicitud>> GetFeedAsync(string? zone, SolicitudStatus? status, CancellationToken ct);
}

public class SolicitudService : ISolicitudService
{
    private readonly AppDbContext _db;
    private readonly IUserService _users;
    private readonly IPushQueue _pushQueue;

    public SolicitudService(AppDbContext db, IUserService users, IPushQueue pushQueue)
    {
        _db = db;
        _users = users;
        _pushQueue = pushQueue;
    }

    /// <summary>Crea la solicitud + todos sus materiales en una sola transacción (rol constructor).</summary>
    public async Task<Solicitud> CreateAsync(string uid, CreateSolicitudRequest req, CancellationToken ct)
    {
        var user = await _users.GetRequiredAsync(uid, ct);
        if (user.Role != UserRole.constructor)
            throw AppException.Forbidden("Sólo un constructor puede crear solicitudes.");

        await using var tx = await _db.Database.BeginTransactionAsync(ct);

        var solicitud = new Solicitud
        {
            Id = Guid.NewGuid(),
            ConstructorId = user.Uid,
            ConstructorName = user.FullName,
            Title = req.Title,
            DeliveryZone = req.DeliveryZone,
            Deadline = req.Deadline,
            Notes = req.Notes,
            AttachmentUrl = req.AttachmentUrl,
            Status = SolicitudStatus.OPEN,
            OfertasCount = 0,
            CorralonesNotifiedCount = 0,
            CreatedAt = DateTime.UtcNow,
            Materiales = req.Materiales.Select(m => new Material
            {
                Id = Guid.NewGuid(),
                Name = m.Name,
                Quantity = m.Quantity,
                Unit = m.Unit,
            }).ToList(),
        };

        _db.Solicitudes.Add(solicitud);

        // Stat denormalizada del constructor.
        user.Stats.TotalLicitaciones += 1;

        // NEW_REQUEST: notificar a los corralones de la zona de entrega.
        var corralonUids = await _db.Users
            .Where(u => u.Role == UserRole.corralon && u.Zone == solicitud.DeliveryZone)
            .Select(u => u.Uid)
            .ToListAsync(ct);

        var notifs = corralonUids
            .Select(corralonUid => NotificationFactory.NewRequest(corralonUid, solicitud))
            .ToList();
        _db.Notifications.AddRange(notifs);

        solicitud.CorralonesNotifiedCount = corralonUids.Count;

        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        // Encolar tras el commit el push a los corralones de la zona.
        _pushQueue.Enqueue(notifs);
        return solicitud;
    }

    public Task<List<Solicitud>> GetMineAsync(string uid, CancellationToken ct)
        => _db.Solicitudes
            .Include(s => s.Materiales)
            .Where(s => s.ConstructorId == uid)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync(ct);

    public async Task<Solicitud> GetByIdAsync(Guid id, CancellationToken ct)
        => await _db.Solicitudes
            .Include(s => s.Materiales)
            .FirstOrDefaultAsync(s => s.Id == id, ct)
           ?? throw AppException.NotFound("Solicitud no encontrada.");

    /// <summary>Feed del corralón: por zona y estado, ordenado por deadline asc.</summary>
    public Task<List<Solicitud>> GetFeedAsync(string? zone, SolicitudStatus? status, CancellationToken ct)
    {
        var q = _db.Solicitudes.Include(s => s.Materiales).AsQueryable();

        if (!string.IsNullOrWhiteSpace(zone))
            q = q.Where(s => s.DeliveryZone == zone);

        if (status is not null)
            q = q.Where(s => s.Status == status);

        return q.OrderBy(s => s.Deadline).ToListAsync(ct);
    }
}
