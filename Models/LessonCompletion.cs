using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NoryMusicLMS_VS.Models;

public class LessonCompletion
{
    public int Id { get; set; }

    [Required]
    public string StudentId { get; set; } = string.Empty;

    [ForeignKey(nameof(StudentId))]
    public ApplicationUser Student { get; set; } = default!;

    public int LessonId { get; set; }

    [ForeignKey(nameof(LessonId))]
    public Lesson Lesson { get; set; } = default!;

    public DateTime CompletedAt { get; set; } = DateTime.UtcNow;
}
