using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NoryMusicLMS_VS.Models
{
    public class QuizAnswer
    {
        public int Id { get; set; }

        [Required]
        public int QuizAttemptId { get; set; }

        [ForeignKey("QuizAttemptId")]
        public QuizAttempt QuizAttempt { get; set; } = default!;

        [Required]
        public int QuizQuestionId { get; set; }

        [ForeignKey("QuizQuestionId")]
        public QuizQuestion Question { get; set; } = default!;

        // Student's answer
        public string? SelectedAnswer { get; set; } // For MC: "B"; TF: "true"; FIB: "C major"
        public int? SelectedAnswerIndex { get; set; } // For MC: index selected

        public bool IsCorrect { get; set; } = false;
        public double PointsEarned { get; set; } = 0;
        public string? Feedback { get; set; } // Instructor feedback
        public DateTime AnsweredAt { get; set; } = DateTime.UtcNow;
    }
}