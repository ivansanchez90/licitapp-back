using LicitApp.Api.Data;
using LicitApp.Api.Domain;
using LicitApp.Api.Dtos;
using Microsoft.EntityFrameworkCore;

namespace LicitApp.Api.Services;

public interface IUserService
{
    Task<User> SyncAsync(string uid, string? email, SyncUserRequest req, CancellationToken ct);
    Task<User> GetRequiredAsync(string uid, CancellationToken ct);
    Task<User?> GetAsync(string uid, CancellationToken ct);
    Task<User> UpdateAsync(string uid, UpdateUserRequest req, CancellationToken ct);
}

public class UserService : IUserService
{
    private readonly AppDbContext _db;

    public UserService(AppDbContext db) => _db = db;

    /// <summary>Upsert del perfil. Crea la fila la primera vez; si existe, actualiza datos del registro.</summary>
    public async Task<User> SyncAsync(string uid, string? email, SyncUserRequest req, CancellationToken ct)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Uid == uid, ct);
        if (user is null)
        {
            user = new User
            {
                Uid = uid,
                Email = email ?? string.Empty,
                CreatedAt = DateTime.UtcNow,
                Stats = new UserStats(),
            };
            _db.Users.Add(user);
        }

        // El email siempre viene del token (fuente de verdad de identidad).
        if (!string.IsNullOrWhiteSpace(email))
            user.Email = email;

        user.Role = req.Role;
        user.FullName = req.FullName;
        user.Phone = req.Phone;
        user.Zone = req.Zone;
        user.BusinessName = req.BusinessName;

        await _db.SaveChangesAsync(ct);
        return user;
    }

    public async Task<User> GetRequiredAsync(string uid, CancellationToken ct)
        => await _db.Users.FirstOrDefaultAsync(u => u.Uid == uid, ct)
           ?? throw AppException.NotFound("Perfil no encontrado. Sincronizá tu cuenta con POST /api/users/sync.");

    public Task<User?> GetAsync(string uid, CancellationToken ct)
        => _db.Users.FirstOrDefaultAsync(u => u.Uid == uid, ct);

    public async Task<User> UpdateAsync(string uid, UpdateUserRequest req, CancellationToken ct)
    {
        var user = await GetRequiredAsync(uid, ct);

        if (req.FullName is not null) user.FullName = req.FullName;
        if (req.Phone is not null) user.Phone = req.Phone;
        if (req.Zone is not null) user.Zone = req.Zone;
        if (req.BusinessName is not null) user.BusinessName = req.BusinessName;
        if (req.PushToken is not null) user.PushToken = req.PushToken;

        await _db.SaveChangesAsync(ct);
        return user;
    }
}
