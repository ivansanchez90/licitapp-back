using LicitApp.Api.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LicitApp.Api.Tests;

/// <summary>
/// Arranca la API real en memoria (TestServer) pero: en entorno "Testing" (sin Postgres,
/// se usa InMemory), con el almacenamiento apuntando a un directorio temporal y la auth
/// reemplazada por <see cref="TestAuthHandler"/>.
/// </summary>
public class FilesApiFactory : WebApplicationFactory<Program>
{
    /// <summary>Directorio temporal donde el endpoint escribe los archivos durante los tests.</summary>
    public string FilesRoot { get; } =
        Path.Combine(Path.GetTempPath(), "licitapp-tests", Guid.NewGuid().ToString("N"));

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["FileStorage:RootPath"] = FilesRoot,
                ["FileStorage:BaseUrl"] = "https://test.local/files",
                // El job de DEADLINE_NEAR no aporta a estos tests y tocaría la DB.
                ["Notifications:DeadlineNear:Enabled"] = "false",
            });
        });

        builder.ConfigureTestServices(services =>
        {
            // Reemplazar Npgsql por EF InMemory.
            var optionsDescriptors = services
                .Where(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>))
                .ToList();
            foreach (var d in optionsDescriptors)
                services.Remove(d);

            services.AddDbContext<AppDbContext>(o => o.UseInMemoryDatabase("files-tests"));

            // Reemplazar JwtBearer por el esquema de test (pasa a ser el default).
            services.AddAuthentication(TestAuthHandler.SchemeName)
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { });
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing && Directory.Exists(FilesRoot))
        {
            try { Directory.Delete(FilesRoot, recursive: true); }
            catch { /* best-effort cleanup */ }
        }
    }
}
