using LicitApp.Api.Data;
using LicitApp.Api.Domain;
using LicitApp.Api.Dtos;
using Microsoft.EntityFrameworkCore;

namespace LicitApp.Api.Services;

public interface IOfertaService
{
    Task<Oferta> CreateAsync(string uid, Guid solicitudId, CreateOfertaRequest req, CancellationToken ct);
    Task<List<Oferta>> GetActiveForSolicitudAsync(Guid solicitudId, CancellationToken ct);
    Task<OfertasResumenDto> GetResumenAsync(Guid solicitudId, CancellationToken ct);
    Task<List<OfertaDto>> GetMineAsync(string uid, CancellationToken ct);
    Task<Oferta> GetOneAsync(Guid solicitudId, Guid ofertaId, CancellationToken ct);
    Task<Oferta> UpdateAsync(string uid, Guid solicitudId, Guid ofertaId, UpdateOfertaRequest req, CancellationToken ct);
    Task<Oferta> WithdrawAsync(string uid, Guid solicitudId, Guid ofertaId, CancellationToken ct);
    Task<Solicitud> AcceptAsync(string uid, Guid solicitudId, Guid ofertaId, CancellationToken ct);
}

public class OfertaService : IOfertaService
{
    private readonly AppDbContext _db;
    private readonly IUserService _users;
    private readonly IPushQueue _pushQueue;

    public OfertaService(AppDbContext db, IUserService users, IPushQueue pushQueue)
    {
        _db = db;
        _users = users;
        _pushQueue = pushQueue;
    }

    /// <summary>Regla #2: crear oferta. Verifica OPEN, recalcula badges, incrementa contadores. Transaccional.</summary>
    public async Task<Oferta> CreateAsync(string uid, Guid solicitudId, CreateOfertaRequest req, CancellationToken ct)
    {
        var corralon = await _users.GetRequiredAsync(uid, ct);
        if (corralon.Role != UserRole.corralon)
            throw AppException.Forbidden("Sólo un corralón puede crear ofertas.");

        ValidateShipping(req.ShippingType, req.ShippingPrice);

        await using var tx = await _db.Database.BeginTransactionAsync(ct);

        var solicitud = await _db.Solicitudes.FirstOrDefaultAsync(s => s.Id == solicitudId, ct)
            ?? throw AppException.NotFound("Solicitud no encontrada.");

        if (solicitud.Status != SolicitudStatus.OPEN)
            throw AppException.Conflict("Esta licitación ya no está activa.");

        var oferta = new Oferta
        {
            Id = Guid.NewGuid(),
            SolicitudId = solicitud.Id,
            CorralonId = corralon.Uid,
            CorralonName = corralon.BusinessName ?? corralon.FullName,
            TotalPrice = req.TotalPrice,
            ShippingType = req.ShippingType,
            ShippingPrice = req.ShippingType == ShippingType.FREE ? null : req.ShippingPrice,
            DeliveryHours = req.DeliveryHours,
            ValidUntil = req.ValidUntil,
            Comment = req.Comment,
            AttachmentUrl = NormalizeAttachmentUrl(req.AttachmentUrl),
            Status = OfertaStatus.ACTIVE,
            IsFastDelivery = req.DeliveryHours <= 24,
            CreatedAt = DateTime.UtcNow,
        };

        _db.Ofertas.Add(oferta);
        solicitud.OfertasCount += 1;
        corralon.Stats.TotalOfertas += 1;

        // NEW_OFFER: notificar al constructor dueño de la solicitud.
        var notif = NotificationFactory.NewOffer(solicitud.ConstructorId, solicitud, oferta);
        _db.Notifications.Add(notif);

        await _db.SaveChangesAsync(ct);

        // Recalcular el badge de mejor precio sobre todas las ACTIVE (incluida la nueva).
        await RecalcBestPriceAsync(solicitud.Id, ct);
        await _db.SaveChangesAsync(ct);

        await tx.CommitAsync(ct);

        // Encolar tras el commit: si la transacción se revierte, no se envía nada.
        _pushQueue.Enqueue(new[] { notif });
        return oferta;
    }

    public Task<List<Oferta>> GetActiveForSolicitudAsync(Guid solicitudId, CancellationToken ct)
        => _db.Ofertas
            .Where(o => o.SolicitudId == solicitudId && o.Status == OfertaStatus.ACTIVE)
            .OrderBy(o => o.TotalPrice)
            .ToListAsync(ct);

    /// <summary>Regla #6: { count, bestPrice } sobre las ofertas ACTIVE.</summary>
    public async Task<OfertasResumenDto> GetResumenAsync(Guid solicitudId, CancellationToken ct)
    {
        var actives = _db.Ofertas.Where(o => o.SolicitudId == solicitudId && o.Status == OfertaStatus.ACTIVE);
        var count = await actives.CountAsync(ct);
        decimal? best = count == 0 ? null : await actives.MinAsync(o => o.TotalPrice, ct);
        return new OfertasResumenDto(count, best);
    }

    /// <summary>Mis ofertas (corralón), createdAt desc, con campos denormalizados de la solicitud.</summary>
    public async Task<List<OfertaDto>> GetMineAsync(string uid, CancellationToken ct)
    {
        var rows = await (
            from o in _db.Ofertas
            join s in _db.Solicitudes on o.SolicitudId equals s.Id
            where o.CorralonId == uid
            orderby o.CreatedAt descending
            select new { Oferta = o, s.Title, s.Deadline }
        ).ToListAsync(ct);

        return rows.Select(r => r.Oferta.ToDto(r.Title, r.Deadline)).ToList();
    }

    public async Task<Oferta> GetOneAsync(Guid solicitudId, Guid ofertaId, CancellationToken ct)
        => await _db.Ofertas.FirstOrDefaultAsync(o => o.Id == ofertaId && o.SolicitudId == solicitudId, ct)
           ?? throw AppException.NotFound("Oferta no encontrada.");

    /// <summary>Regla #4: editar oferta. Sólo si la solicitud sigue OPEN y el corralón es dueño.</summary>
    public async Task<Oferta> UpdateAsync(string uid, Guid solicitudId, Guid ofertaId, UpdateOfertaRequest req, CancellationToken ct)
    {
        ValidateShipping(req.ShippingType, req.ShippingPrice);

        await using var tx = await _db.Database.BeginTransactionAsync(ct);

        var solicitud = await _db.Solicitudes.FirstOrDefaultAsync(s => s.Id == solicitudId, ct)
            ?? throw AppException.NotFound("Solicitud no encontrada.");

        var oferta = await _db.Ofertas.FirstOrDefaultAsync(o => o.Id == ofertaId && o.SolicitudId == solicitudId, ct)
            ?? throw AppException.NotFound("Oferta no encontrada.");

        if (oferta.CorralonId != uid)
            throw AppException.Forbidden("Sólo el corralón dueño puede editar esta oferta.");

        if (solicitud.Status != SolicitudStatus.OPEN)
            throw AppException.Conflict("Esta licitación ya no está activa.");

        if (oferta.Status != OfertaStatus.ACTIVE)
            throw AppException.Conflict("Sólo se puede editar una oferta activa.");

        oferta.TotalPrice = req.TotalPrice;
        oferta.ShippingType = req.ShippingType;
        oferta.ShippingPrice = req.ShippingType == ShippingType.FREE ? null : req.ShippingPrice;
        oferta.DeliveryHours = req.DeliveryHours;
        oferta.IsFastDelivery = req.DeliveryHours <= 24;
        oferta.Comment = req.Comment;
        // PUT con semántica de reemplazo: null/vacío borra el adjunto.
        oferta.AttachmentUrl = NormalizeAttachmentUrl(req.AttachmentUrl);

        await _db.SaveChangesAsync(ct);

        // El precio pudo cambiar: recalcular el badge de mejor precio.
        await RecalcBestPriceAsync(solicitudId, ct);
        await _db.SaveChangesAsync(ct);

        await tx.CommitAsync(ct);
        return oferta;
    }

    /// <summary>Regla #5: retirar oferta -> WITHDRAWN. Sólo el corralón dueño.</summary>
    public async Task<Oferta> WithdrawAsync(string uid, Guid solicitudId, Guid ofertaId, CancellationToken ct)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(ct);

        var oferta = await _db.Ofertas.FirstOrDefaultAsync(o => o.Id == ofertaId && o.SolicitudId == solicitudId, ct)
            ?? throw AppException.NotFound("Oferta no encontrada.");

        if (oferta.CorralonId != uid)
            throw AppException.Forbidden("Sólo el corralón dueño puede retirar esta oferta.");

        if (oferta.Status != OfertaStatus.ACTIVE)
            throw AppException.Conflict("La oferta no está activa.");

        oferta.Status = OfertaStatus.WITHDRAWN;
        oferta.IsBestPrice = false;

        var solicitud = await _db.Solicitudes.FirstOrDefaultAsync(s => s.Id == solicitudId, ct);
        if (solicitud is not null && solicitud.OfertasCount > 0)
            solicitud.OfertasCount -= 1;

        await _db.SaveChangesAsync(ct);

        // Recalcular el badge entre las que siguen activas.
        await RecalcBestPriceAsync(solicitudId, ct);
        await _db.SaveChangesAsync(ct);

        await tx.CommitAsync(ct);
        return oferta;
    }

    /// <summary>Regla #3: aceptar oferta. Cierra la licitación de forma atómica.</summary>
    public async Task<Solicitud> AcceptAsync(string uid, Guid solicitudId, Guid ofertaId, CancellationToken ct)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(ct);

        var solicitud = await _db.Solicitudes
            .Include(s => s.Ofertas)
            .FirstOrDefaultAsync(s => s.Id == solicitudId, ct)
            ?? throw AppException.NotFound("Solicitud no encontrada.");

        if (solicitud.ConstructorId != uid)
            throw AppException.Forbidden("Sólo el constructor dueño puede aceptar una oferta.");

        if (solicitud.Status != SolicitudStatus.OPEN)
            throw AppException.Conflict("Esta licitación ya no está disponible.");

        var ganadora = solicitud.Ofertas.FirstOrDefault(o => o.Id == ofertaId)
            ?? throw AppException.NotFound("Oferta no encontrada.");

        if (ganadora.Status != OfertaStatus.ACTIVE)
            throw AppException.Conflict("La oferta seleccionada no está activa.");

        // Ganadora -> WON; el resto de las ACTIVE -> LOST. Notificar a cada corralón.
        var notifs = new List<Notification>();
        foreach (var o in solicitud.Ofertas.Where(o => o.Status == OfertaStatus.ACTIVE).ToList())
        {
            if (o.Id == ganadora.Id)
            {
                o.Status = OfertaStatus.WON;
                notifs.Add(NotificationFactory.OfferWon(solicitud, o));
            }
            else
            {
                o.Status = OfertaStatus.LOST;
                notifs.Add(NotificationFactory.OfferLost(solicitud, o));
            }
        }
        _db.Notifications.AddRange(notifs);

        solicitud.Status = SolicitudStatus.CLOSED;
        solicitud.WinningOfferId = ganadora.Id;

        // Incrementar cierres del constructor y del corralón ganador (siempre usuarios distintos).
        var constructor = await _users.GetRequiredAsync(solicitud.ConstructorId, ct);
        constructor.Stats.TotalCierres += 1;

        var corralon = await _users.GetRequiredAsync(ganadora.CorralonId, ct);
        corralon.Stats.TotalCierres += 1;

        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        // Encolar tras el commit el push a ganador (WON) y perdedores (LOST).
        _pushQueue.Enqueue(notifs);
        return solicitud;
    }

    /// <summary>
    /// El badge isBestPrice pertenece a la única oferta ACTIVE más barata
    /// (desempate: la más reciente, según la regla "la nueva con &lt;= se queda el badge").
    /// </summary>
    private async Task RecalcBestPriceAsync(Guid solicitudId, CancellationToken ct)
    {
        var actives = await _db.Ofertas
            .Where(o => o.SolicitudId == solicitudId && o.Status == OfertaStatus.ACTIVE)
            .ToListAsync(ct);

        var winner = actives
            .OrderBy(o => o.TotalPrice)
            .ThenByDescending(o => o.CreatedAt)
            .FirstOrDefault();

        foreach (var o in actives)
            o.IsBestPrice = winner is not null && o.Id == winner.Id;
    }

    private static void ValidateShipping(ShippingType type, decimal? price)
    {
        if (type is ShippingType.CHARGED or ShippingType.FIXED_PRICE && price is null or <= 0)
            throw AppException.BadRequest("El envío con cargo requiere un shippingPrice mayor a 0.");
    }

    /// <summary>
    /// Normaliza el adjunto: vacío/whitespace -> null (borra el adjunto). Si trae valor,
    /// debe ser una URL absoluta bien formada; si no, 400. El backend no valida que el
    /// archivo exista en Storage ni que sea de un proveedor en particular.
    /// </summary>
    private static string? NormalizeAttachmentUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return null;

        var trimmed = url.Trim();
        if (!Uri.IsWellFormedUriString(trimmed, UriKind.Absolute))
            throw AppException.BadRequest("El adjunto debe ser una URL absoluta válida.");

        return trimmed;
    }
}
