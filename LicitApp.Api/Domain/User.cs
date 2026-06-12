using System.ComponentModel.DataAnnotations;

namespace LicitApp.Api.Domain;

/// <summary>
/// Perfil de negocio. La PK es el Firebase UID (claim "sub" del ID Token).
/// Firebase es la única fuente de identidad; acá guardamos rol, zona y stats.
/// </summary>
public class User
{
    // Firebase UID. No es autogenerado: viene del token.
    [Key]
    public string Uid { get; set; } = default!;

    public string Email { get; set; } = default!;
    public string FullName { get; set; } = default!;
    public UserRole Role { get; set; }
    public string Phone { get; set; } = default!;
    public string Zone { get; set; } = default!;

    // Sólo los corralones lo usan.
    public string? BusinessName { get; set; }

    public bool Verified { get; set; }

    // Token de Expo Notifications.
    public string? PushToken { get; set; }

    public DateTime CreatedAt { get; set; }

    // Modelado como owned entity -> columnas stats_total_licitaciones, etc.
    public UserStats Stats { get; set; } = new();
}

/// <summary>
/// El front lo consume como objeto anidado: stats: { totalLicitaciones, totalOfertas, totalCierres }.
/// </summary>
public class UserStats
{
    public int TotalLicitaciones { get; set; }
    public int TotalOfertas { get; set; }
    public int TotalCierres { get; set; }
}
