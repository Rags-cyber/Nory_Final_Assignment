using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NoryMusicLMS_VS.Models
{
    public enum ResourceType
    {
        Pdf,
        SheetMusic,
        AudioTrack,
        Video,
        ExternalLink,
        Other
    }

    
    
    public class Resource
    {
        public int Id { get; set; }

        [Required, StringLength(150)]
        public string Title { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        public ResourceType Type { get; set; } = ResourceType.Other;

        
        [Required, StringLength(1000)]
        public string Url { get; set; } = string.Empty;

        [Required]
        public int LessonId { get; set; }

        [ForeignKey("LessonId")]
        public Lesson Lesson { get; set; } = default!;

        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    }
}
