using Microsoft.EntityFrameworkCore;
using NoryMusicLMS_VS.Data;

namespace NoryMusicLMS_VS.Services;

public class AudioStorageService
{
    public const long MaxBytes = 10 * 1024 * 1024;
    public const string UrlPrefix = "/media/";

    private readonly ApplicationDbContext _db;

    public AudioStorageService(ApplicationDbContext db)
    {
        _db = db;
    }

    public static string? Validate(IFormFile? file)
    {
        if (file is null || file.Length == 0)
            return null;
        if (file.Length > MaxBytes)
            return $"Audio is too large. The maximum size is {MaxBytes / (1024 * 1024)} MB.";
        using var stream = file.OpenReadStream();
        var header = new byte[12];
        var read = stream.Read(header, 0, header.Length);
        return DetectContentType(header.AsSpan(0, read)) is null
            ? "Please upload an MP3 audio file."
            : null;
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

        return currentUrl ?? string.Empty;
    }

    public async Task<string> SaveAsync(IFormFile file, string? userId)
    {
        await using var buffer = new MemoryStream((int)file.Length);
        await file.CopyToAsync(buffer);
        var bytes = buffer.ToArray();
        var contentType = DetectContentType(bytes) ?? throw new InvalidOperationException("Unsupported audio type.");
        var media = new Models.StoredImage
        {
            FileName = Path.GetFileName(file.FileName) is { Length: > 0 } name
                ? (name.Length > 255 ? name[^255..] : name)
                : "audio.mp3",
            ContentType = contentType,
            SizeBytes = bytes.LongLength,
            Data = bytes,
            UploadedAt = DateTime.UtcNow,
            UploadedById = userId
        };
        _db.StoredImages.Add(media);
        await _db.SaveChangesAsync();
        return UrlPrefix + media.Id;
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

    private static string? DetectContentType(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length >= 3 && bytes[0] == 0x49 && bytes[1] == 0x44 && bytes[2] == 0x33)
            return "audio/mpeg";
        if (bytes.Length >= 2 && bytes[0] == 0xFF && (bytes[1] & 0xE0) == 0xE0)
            return "audio/mpeg";
        return null;
    }
}
