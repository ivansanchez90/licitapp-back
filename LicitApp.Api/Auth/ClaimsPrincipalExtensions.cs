using System.Security.Claims;

namespace LicitApp.Api.Auth;

public static class ClaimsPrincipalExtensions
{
    /// <summary>
    /// Firebase UID. En los ID Tokens de Firebase viene tanto en "user_id" como en "sub"
    /// (que JwtBearer mapea a ClaimTypes.NameIdentifier).
    /// </summary>
    public static string? GetUid(this ClaimsPrincipal user)
        => user.FindFirstValue("user_id")
           ?? user.FindFirstValue(ClaimTypes.NameIdentifier)
           ?? user.FindFirstValue("sub");

    public static string? GetEmail(this ClaimsPrincipal user)
        => user.FindFirstValue("email")
           ?? user.FindFirstValue(ClaimTypes.Email);

    /// <summary>Devuelve el UID o lanza si no hay (no debería pasar tras [Authorize]).</summary>
    public static string RequireUid(this ClaimsPrincipal user)
        => user.GetUid() ?? throw new UnauthorizedAccessException("Token sin UID.");
}
