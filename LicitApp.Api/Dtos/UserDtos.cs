using System.ComponentModel.DataAnnotations;
using LicitApp.Api.Domain;

namespace LicitApp.Api.Dtos;

public record StatsDto(int TotalLicitaciones, int TotalOfertas, int TotalCierres);

public record UserDto(
    string Uid,
    string Email,
    string FullName,
    UserRole Role,
    string Phone,
    string Zone,
    string? BusinessName,
    bool Verified,
    string? PushToken,
    DateTime CreatedAt,
    StatsDto Stats);

/// <summary>
/// Body de POST /api/users/sync. El email/uid salen del token; el resto del registro.
/// </summary>
public class SyncUserRequest
{
    [Required]
    public UserRole Role { get; set; }

    [Required, MaxLength(160)]
    public string FullName { get; set; } = default!;

    [Required, MaxLength(40)]
    public string Phone { get; set; } = default!;

    [Required, MaxLength(120)]
    public string Zone { get; set; } = default!;

    [MaxLength(160)]
    public string? BusinessName { get; set; }
}

/// <summary>
/// Body de PUT /api/users/me. Todos opcionales (patch parcial).
/// </summary>
public class UpdateUserRequest
{
    [MaxLength(160)]
    public string? FullName { get; set; }

    [MaxLength(40)]
    public string? Phone { get; set; }

    [MaxLength(120)]
    public string? Zone { get; set; }

    [MaxLength(160)]
    public string? BusinessName { get; set; }

    public string? PushToken { get; set; }
}
