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
        public string Content { get; set; } = string.Empty; 

        [Required]
        public int CourseId { get; set; }

        [ForeignKey("CourseId")]
        public Course Course { get; set; } = default!;

        public int OrderIndex { get; set; } = 0;
        public string? VideoUrl { get; set; } 
        public string? AudioUrl { get; set; } 
        public string? NotationUrl { get; set; } 

        
        public string? ImageUrl { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        
        public ICollection<Quiz> Quizzes { get; set; } = new List<Quiz>();
        public ICollection<Resource> Resources { get; set; } = new List<Resource>();
    }
}