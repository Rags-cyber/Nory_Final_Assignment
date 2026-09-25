using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NoryMusicLMS_VS.Models
{
    public class Lesson
    {
        public int Id { get; set; }

        [Required, StringLength(100)]
        public string Title { get; set; } = string.Empty;

        [StringLength(2000)]
        public string Content { get; set; } = string.Empty; // HTML/markdown content

        [Required]
        public int CourseId { get; set; }

        [ForeignKey("CourseId")]
        public Course Course { get; set; } = default!;

        public int OrderIndex { get; set; } = 0;
        public string? VideoUrl { get; set; } // YouTube/Vimeo link or local video
        public string? AudioUrl { get; set; } // For theory examples
        public string? NotationUrl { get; set; } // Link to sheet music/image
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public ICollection<Quiz> Quizzes { get; set; } = new List<Quiz>();
        public ICollection<Resource> Resources { get; set; } = new List<Resource>();
    }
}