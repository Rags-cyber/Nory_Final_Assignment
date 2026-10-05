using Microsoft.AspNetCore.Identity;

namespace NoryMusicLMS_VS.Models
{
    
    public class ApplicationUser : IdentityUser
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public DateTime DateOfBirth { get; set; } = new DateTime(2000, 1, 1);
        public string? Bio { get; set; }
        public string? Instrument { get; set; }
        public string? SkillLevel { get; set; }
        public string? LearningGoals { get; set; }
        public string? TeachingSpecialty { get; set; }
        public int? YearsTeaching { get; set; }
        public string? AdminDepartment { get; set; }
        public string? AdminJobTitle { get; set; }

        
        public ICollection<Course> InstructorCourses { get; set; } = new List<Course>();
        public ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
        public ICollection<QuizAttempt> QuizAttempts { get; set; } = new List<QuizAttempt>();
    }
}
