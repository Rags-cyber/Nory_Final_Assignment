using Microsoft.EntityFrameworkCore;
using NoryMusicLMS_VS.Data;
using NoryMusicLMS_VS.Models;

namespace NoryMusicLMS_VS.Services;





public class ImageStorageService
{
    public const long MaxBytes = 2 * 1024 * 1024; 
    public const string UrlPrefix = "/media/";
    public const string AcceptAttribute = "image/png,image/jpeg,image/gif,image/webp";

    private readonly ApplicationDbContext _db;
    public ImageStorageService(ApplicationDbContext db) => _db = db;

    
    public static string? Validate(IFormFile? file)
    {
        if (file is null || file.Length == 0) return null;
        if (file.Length > MaxBytes)
            return $"Image is too large. The maximum size is {MaxBytes / (1024 * 1024)} MB.";

        using var stream = file.OpenReadStream();
        var header = new byte[12];
        var read = stream.Read(header, 0, header.Length);
        return DetectContentType(header.AsSpan(0, read)) is null
            ? "Please upload a PNG, JPG, GIF or WebP image."
            : null;
    }

    
    public async Task<string> SaveAsync(IFormFile file, string? userId)
    {
        await using var buffer = new MemoryStream((int)file.Length);
        await file.CopyToAsync(buffer);
        var bytes = buffer.ToArray();

        
        var contentType = DetectContentType(bytes)
            ?? throw new InvalidOperationException("Unsupported image type.");

        var image = new StoredImage
        {
            FileName = Path.GetFileName(file.FileName) is { Length: > 0 } name
                ? (name.Length > 255 ? name[^255..] : name)
                : "image",
            ContentType = contentType,
            SizeBytes = bytes.LongLength,
            Data = bytes,
            UploadedAt = DateTime.UtcNow,
            UploadedById = userId
        };
        _db.StoredImages.Add(image);
        await _db.SaveChangesAsync();
        return UrlPrefix + image.Id;
    }

    
    
    
    
    public async Task<string?> ApplyAsync(string? currentUrl, IFormFile? upload, bool remove, string? userId)
    {
        if (upload is { Length: > 0 })
        {
            var newUrl = await SaveAsync(upload, userId);
            await DeleteIfStoredAsync(currentUrl);
            return newUrl;
        }

        if (remove)
        {
            await DeleteIfStoredAsync(currentUrl);
            return null;
        }

        return currentUrl;
    }

    
    public async Task DeleteIfStoredAsync(string? url)
    {
        if (TryGetId(url, out var id))
            await _db.StoredImages.Where(i => i.Id == id).ExecuteDeleteAsync();
    }

    public static bool TryGetId(string? url, out int id)
    {
        id = 0;
        return url is not null
            && url.StartsWith(UrlPrefix, StringComparison.Ordinal)
            && int.TryParse(url.AsSpan(UrlPrefix.Length), out id);
    }

    private static string? DetectContentType(ReadOnlySpan<byte> b)
    {
        if (b.Length >= 8 && b[0] == 0x89 && b[1] == 0x50 && b[2] == 0x4E && b[3] == 0x47
            && b[4] == 0x0D && b[5] == 0x0A && b[6] == 0x1A && b[7] == 0x0A) return "image/png";
        if (b.Length >= 3 && b[0] == 0xFF && b[1] == 0xD8 && b[2] == 0xFF) return "image/jpeg";
        if (b.Length >= 6 && b[0] == 'G' && b[1] == 'I' && b[2] == 'F' && b[3] == '8'
            && (b[4] == '7' || b[4] == '9') && b[5] == 'a') return "image/gif";
        if (b.Length >= 12 && b[0] == 'R' && b[1] == 'I' && b[2] == 'F' && b[3] == 'F'
            && b[8] == 'W' && b[9] == 'E' && b[10] == 'B' && b[11] == 'P') return "image/webp";
        return null; 
    }
}
