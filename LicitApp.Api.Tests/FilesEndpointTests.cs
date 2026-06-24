using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Xunit;

namespace LicitApp.Api.Tests;

public class FilesEndpointTests : IClassFixture<FilesApiFactory>
{
    private const string Uid = "abc123";
    private readonly FilesApiFactory _factory;

    public FilesEndpointTests(FilesApiFactory factory) => _factory = factory;

    private sealed record UrlResponse(string Url);

    /// <summary>Cliente con (o sin) el header de UID que usa el TestAuthHandler.</summary>
    private HttpClient Client(string? uid = Uid)
    {
        var client = _factory.CreateClient();
        if (uid is not null)
            client.DefaultRequestHeaders.Add(TestAuthHandler.UidHeader, uid);
        return client;
    }

    private static MultipartFormDataContent Multipart(byte[] bytes, string contentType, string fileName, string path)
    {
        var file = new ByteArrayContent(bytes);
        file.Headers.ContentType = new MediaTypeHeaderValue(contentType);

        return new MultipartFormDataContent
        {
            { file, "file", fileName },
            { new StringContent(path), "path" },
        };
    }

    private static byte[] RandomBytes(int length)
    {
        var bytes = new byte[length];
        new Random(1234).NextBytes(bytes);
        return bytes;
    }

    [Fact]
    public async Task Post_ValidPdf_Returns200_WritesToDisk_AndServesBytes()
    {
        var bytes = RandomBytes(2048);
        var path = $"attachments/{Uid}/1718000000_presupuesto.pdf";

        var resp = await Client().PostAsync("/api/files",
            Multipart(bytes, "application/pdf", "presupuesto.pdf", path));

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var body = await resp.Content.ReadFromJsonAsync<UrlResponse>();
        Assert.NotNull(body);
        Assert.EndsWith(path, body!.Url);

        // El archivo quedó escrito en disco.
        Assert.True(File.Exists(Path.Combine(_factory.FilesRoot, path)));

        // Y se sirve como estático bajo /files (sin auth), con los bytes originales.
        var served = await Client(uid: null).GetAsync(new Uri(body.Url).AbsolutePath);
        Assert.Equal(HttpStatusCode.OK, served.StatusCode);
        Assert.Equal(bytes, await served.Content.ReadAsByteArrayAsync());
    }

    [Fact]
    public async Task Post_ValidJpg_Returns200()
    {
        var path = $"ofertas-attachments/{Uid}/1718000000_foto.jpg";

        var resp = await Client().PostAsync("/api/files",
            Multipart(RandomBytes(1024), "image/jpeg", "foto.jpg", path));

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
    }

    [Fact]
    public async Task Post_WithoutToken_Returns401()
    {
        var path = $"attachments/{Uid}/1718000000_x.pdf";

        var resp = await Client(uid: null).PostAsync("/api/files",
            Multipart(RandomBytes(512), "application/pdf", "x.pdf", path));

        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
    }

    [Fact]
    public async Task Post_PathWithInvalidPrefix_Returns403()
    {
        var path = $"otracosa/{Uid}/1718000000_x.pdf";

        var resp = await Client().PostAsync("/api/files",
            Multipart(RandomBytes(512), "application/pdf", "x.pdf", path));

        Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
    }

    [Fact]
    public async Task Post_PathUidDoesNotMatchToken_Returns403()
    {
        // Token de "abc123" intentando escribir en el namespace de "otrouid".
        var path = "attachments/otrouid/1718000000_x.pdf";

        var resp = await Client().PostAsync("/api/files",
            Multipart(RandomBytes(512), "application/pdf", "x.pdf", path));

        Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
    }

    [Fact]
    public async Task Post_PathTraversal_Returns400()
    {
        var path = $"attachments/{Uid}/../../../etc/passwd";

        var resp = await Client().PostAsync("/api/files",
            Multipart(RandomBytes(512), "application/pdf", "passwd", path));

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task Post_FileTooLarge_Returns413()
    {
        // 10.5 MB: por encima del límite del endpoint (10 MB) pero por debajo del
        // límite del pipeline (11 MB), así llega al handler y devuelve 413.
        var bytes = RandomBytes(10 * 1024 * 1024 + 512 * 1024);
        var path = $"attachments/{Uid}/1718000000_grande.pdf";

        var resp = await Client().PostAsync("/api/files",
            Multipart(bytes, "application/pdf", "grande.pdf", path));

        Assert.Equal(HttpStatusCode.RequestEntityTooLarge, resp.StatusCode);
    }

    [Theory]
    [InlineData("application/x-msdownload", "malware.exe")]
    [InlineData("application/zip", "archivo.zip")]
    public async Task Post_DisallowedContentType_Returns415(string contentType, string fileName)
    {
        var path = $"attachments/{Uid}/1718000000_{fileName}";

        var resp = await Client().PostAsync("/api/files",
            Multipart(RandomBytes(512), contentType, fileName, path));

        Assert.Equal(HttpStatusCode.UnsupportedMediaType, resp.StatusCode);
    }

    [Fact]
    public async Task Post_WeirdFileName_IsSanitized()
    {
        var path = $"attachments/{Uid}/1718000000_mi presupuesto (1).pdf";
        var expectedTail = $"attachments/{Uid}/1718000000_mi_presupuesto__1_.pdf";

        var resp = await Client().PostAsync("/api/files",
            Multipart(RandomBytes(512), "application/pdf", "mi presupuesto (1).pdf", path));

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var body = await resp.Content.ReadFromJsonAsync<UrlResponse>();
        Assert.NotNull(body);
        Assert.EndsWith(expectedTail, body!.Url);
        Assert.True(File.Exists(Path.Combine(_factory.FilesRoot, expectedTail)));
    }
}
