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

        
        public string? SelectedAnswer { get; set; } 
        public int? SelectedAnswerIndex { get; set; } 

        public bool IsCorrect { get; set; } = false;
        public double PointsEarned { get; set; } = 0;
        public string? Feedback { get; set; } 
        public DateTime AnsweredAt { get; set; } = DateTime.UtcNow;
    }
}