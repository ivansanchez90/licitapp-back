using System.Text.RegularExpressions;
using LicitApp.Api.Auth;
using LicitApp.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace LicitApp.Api.Controllers;

[ApiController]
[Authorize]
public class FilesController : ControllerBase
{
    private const long MaxFileBytes = 10 * 1024 * 1024; // 10 MB

    private readonly FileStorageOptions _options;
    private readonly ILogger<FilesController> _logger;

    public FilesController(IOptions<FileStorageOptions> options, ILogger<FilesController> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    /// <summary>
    /// Sube un archivo (multipart) y devuelve la URL pública con la que se sirve.
    /// Reemplaza el almacenamiento en Firebase Storage. El archivo queda accesible
    /// sin auth bajo /files/{path}.
    /// </summary>
    [HttpPost("/api/files")]
    [RequestSizeLimit(11 * 1024 * 1024)]
    [RequestFormLimits(MultipartBodyLengthLimit = 11 * 1024 * 1024)]
    [Consumes("multipart/form-data")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(UploadFileResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> UploadFile(IFormFile? file, [FromForm] string? path, CancellationToken ct)
    {
        // 1. Presencia de los campos.
        if (file is null || file.Length == 0)
            return BadRequest(new { error = "Falta el archivo 'file'." });

        if (string.IsNullOrWhiteSpace(path))
            return BadRequest(new { error = "Falta el campo 'path'." });

        // 2. Tamaño máximo (10 MB).
        if (file.Length > MaxFileBytes)
            return StatusCode(StatusCodes.Status413PayloadTooLarge,
                new { error = "El archivo supera el máximo de 10 MB." });

        // 3. Content-type: sólo PDF o imágenes.
        var contentType = file.ContentType ?? string.Empty;
        var allowed = contentType == "application/pdf"
                      || contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase);
        if (!allowed)
            return StatusCode(StatusCodes.Status415UnsupportedMediaType,
                new { error = "Sólo se permiten archivos PDF o imágenes." });

        // 4. El path debe empezar con un prefijo válido + el UID del token.
        var uid = User.RequireUid();
        var validPrefixes = new[] { $"attachments/{uid}/", $"ofertas-attachments/{uid}/" };
        if (!validPrefixes.Any(p => path.StartsWith(p, StringComparison.Ordinal)))
            return StatusCode(StatusCodes.Status403Forbidden,
                new { error = "El path no corresponde a tu usuario." });

        // 5. Sanitizar el nombre (último segmento): sólo [A-Za-z0-9._-].
        var segments = path.Split('/');
        segments[^1] = Regex.Replace(segments[^1], "[^A-Za-z0-9._-]", "_");
        var safePath = string.Join('/', segments);

        // 6. Defensa contra path traversal: el destino real debe quedar dentro del root.
        var rootFull = Path.GetFullPath(_options.RootPath);
        var targetFull = Path.GetFullPath(Path.Combine(_options.RootPath, safePath));
        if (!targetFull.StartsWith(rootFull + Path.DirectorySeparatorChar, StringComparison.Ordinal))
            return BadRequest(new { error = "Path inválido." });

        // 7. Escribir a disco (sobreescribe si ya existe; no guardamos historial).
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(targetFull)!);
            await using var stream = System.IO.File.Create(targetFull);
            await file.CopyToAsync(stream, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error escribiendo el archivo subido a {Target}.", targetFull);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "No se pudo guardar el archivo." });
        }

        var url = $"{_options.BaseUrl.TrimEnd('/')}/{safePath}";
        return Ok(new UploadFileResponse(url));
    }
}

/// <summary>Respuesta de <c>POST /api/files</c>.</summary>
public record UploadFileResponse(string Url);
