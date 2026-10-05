using System.ComponentModel.DataAnnotations;

namespace NoryMusicLMS_VS.Models;







public class StoredImage
{
    public int Id { get; set; }

    [Required, StringLength(255)]
    public string FileName { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string ContentType { get; set; } = string.Empty;

    public long SizeBytes { get; set; }

    [Required]
    public byte[] Data { get; set; } = Array.Empty<byte>();

    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

    [StringLength(450)]
    public string? UploadedById { get; set; }
}
