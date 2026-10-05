using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NoryMusicLMS_VS.Models
{
    public enum QuestionType
    {
        MultipleChoice,
        TrueFalse,
        FillInBlank,
        AudioIdentification 
    }

    public class QuizQuestion
    {
        public int Id { get; set; }

        [Required]
        public int QuizId { get; set; }

        [ForeignKey("QuizId")]
        public Quiz Quiz { get; set; } = default!;

        [Required, StringLength(500)]
        public string QuestionText { get; set; } = string.Empty;

        public QuestionType Type { get; set; } = QuestionType.MultipleChoice;

        
        public string? AudioUrl { get; set; }

        
        
        
        
        [StringLength(200)]
        public string? ToneSequence { get; set; }

        
        public int? CorrectAnswerIndex { get; set; }

        
        
        
        public string? ImageUrl { get; set; }

        
        public bool? CorrectAnswer { get; set; }

        
        public string? CorrectAnswerText { get; set; }

        
        
        public string? OptionsJson { get; set; }

        public int Points { get; set; } = 1;
        public int OrderIndex { get; set; } = 0;

        
        public ICollection<QuizAnswer> Answers { get; set; } = new List<QuizAnswer>();
    }
}
