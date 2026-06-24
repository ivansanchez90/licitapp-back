namespace LicitApp.Api.Services;

/// <summary>Opciones del almacenamiento de archivos en disco (sección "FileStorage").</summary>
public class FileStorageOptions
{
    /// <summary>Directorio físico raíz donde se escriben los uploads.</summary>
    public string RootPath { get; set; } = "/var/lib/licitapp/files";

    /// <summary>Base pública con la que se arma la URL devuelta (se sirve bajo /files).</summary>
    public string BaseUrl { get; set; } = "https://licitapp-api.blackandred.com.ar/files";
}
