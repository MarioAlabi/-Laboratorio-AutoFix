using AutoFix.Api.Exceptions;

namespace AutoFix.Api.Services;

public class ImagenStorageService
{
    private static readonly string[] Permitidas = [".jpg", ".jpeg", ".png"];
    private const long MaxBytes = 5 * 1024 * 1024;
    private readonly IWebHostEnvironment _env;

    public ImagenStorageService(IWebHostEnvironment env)
    {
        _env = env;
    }

    public async Task<string> GuardarImagenAsync(IFormFile? archivo)
    {
        if (archivo is null || archivo.Length == 0)
            throw new BusinessException("Debe adjuntar un archivo de imagen.");

        var ext = Path.GetExtension(archivo.FileName).ToLowerInvariant();
        if (!Permitidas.Contains(ext))
            throw new BusinessException("Solo se permiten JPG, JPEG y PNG.");

        if (archivo.Length > MaxBytes)
            throw new BusinessException("La imagen no puede superar 5 MB.");

        var webRoot = _env.WebRootPath;
        if (string.IsNullOrWhiteSpace(webRoot))
            webRoot = Path.Combine(_env.ContentRootPath, "wwwroot");

        var uploadsDir = Path.Combine(webRoot, "uploads");
        Directory.CreateDirectory(uploadsDir);

        var nombre = $"{Guid.NewGuid()}{ext}";
        var rutaFisica = Path.Combine(uploadsDir, nombre);

        await using var stream = File.Create(rutaFisica);
        await archivo.CopyToAsync(stream);

        return $"uploads/{nombre}";
    }
}
