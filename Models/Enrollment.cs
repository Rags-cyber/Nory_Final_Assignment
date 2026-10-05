using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NoryMusicLMS_VS.Models
{
    public enum EnrollmentStatus
    {
        Active,
        Completed,
        Dropped
    }

    public class Enrollment
    {
        public int Id { get; set; }

        [Required]
        public string StudentId { get; set; } = string.Empty;

        [ForeignKey("StudentId")]
        public ApplicationUser Student { get; set; } = default!;

        [Required]
        public int CourseId { get; set; }

        [ForeignKey("CourseId")]
        public Course Course { get; set; } = default!;

        public DateTime EnrolledAt { get; set; } = DateTime.UtcNow;
        public DateTime? CompletedAt { get; set; }
        public EnrollmentStatus Status { get; set; } = EnrollmentStatus.Active;
        public double ProgressPercentage { get; set; } = 0; 
        public string? CertificateUrl { get; set; } 
    }
}