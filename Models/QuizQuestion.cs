using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NoryMusicLMS_VS.Models
{
    public enum QuestionType
    {
        MultipleChoice,
        TrueFalse,
        FillInBlank,
        AudioIdentification // For ear training: "Which note is this?"
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

        // For audio questions: URL to a real, hosted audio clip
        public string? AudioUrl { get; set; }

        // For synthesized ear-training questions: comma-separated note
        // frequencies in Hz, played in sequence via the Web Audio API in
        // the browser (e.g. "261.63,392.00" plays C4 then G4 - a Perfect
        // 5th). Used instead of AudioUrl when there's no hosted audio file.
        [StringLength(200)]
        public string? ToneSequence { get; set; }

        // For multiple choice: correct answer index (0-based)
        public int? CorrectAnswerIndex { get; set; }

        // For true/false: correct answer
        public bool? CorrectAnswer { get; set; }

        // For fill-in-blank: expected answer
        public string? CorrectAnswerText { get; set; }

        // Options for multiple choice (JSON array of strings)
        // Example: ["C", "D", "E", "F"]
        public string? OptionsJson { get; set; }

        public int Points { get; set; } = 1;
        public int OrderIndex { get; set; } = 0;

        // Navigation property
        public ICollection<QuizAnswer> Answers { get; set; } = new List<QuizAnswer>();
    }
}
