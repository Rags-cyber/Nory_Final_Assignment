using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using NoryMusicLMS_VS.Models;

namespace NoryMusicLMS_VS.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // Music-specific entities
        public DbSet<Course> Courses { get; set; } = default!;
        public DbSet<Lesson> Lessons { get; set; } = default!;
        public DbSet<Quiz> Quizzes { get; set; } = default!;
        public DbSet<QuizQuestion> QuizQuestions { get; set; } = default!;
        public DbSet<QuizAttempt> QuizAttempts { get; set; } = default!;
        public DbSet<QuizAnswer> QuizAnswers { get; set; } = default!;
        public DbSet<Enrollment> Enrollments { get; set; } = default!;
        public DbSet<Resource> Resources { get; set; } = default!;
        public DbSet<LessonCompletion> LessonCompletions { get; set; } = default!;
        public DbSet<StudentAward> StudentAwards { get; set; } = default!;
        public DbSet<ChordSong> ChordSongs { get; set; } = default!;

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Configure relationships
            builder.Entity<Course>()
                .HasOne(c => c.Instructor)
                .WithMany(u => u.InstructorCourses)
                .HasForeignKey(c => c.InstructorId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Lesson>()
                .HasOne(l => l.Course)
                .WithMany(c => c.Lessons)
                .HasForeignKey(l => l.CourseId);

            builder.Entity<Quiz>()
                .HasOne(q => q.Lesson)
                .WithMany(l => l.Quizzes)
                .HasForeignKey(q => q.LessonId);

            builder.Entity<QuizQuestion>()
                .HasOne(qq => qq.Quiz)
                .WithMany(q => q.Questions)
                .HasForeignKey(qq => qq.QuizId);

            builder.Entity<QuizAttempt>()
                .HasOne(qa => qa.Student)
                .WithMany(u => u.QuizAttempts)
                .HasForeignKey(qa => qa.StudentId);

            builder.Entity<QuizAttempt>()
                .HasOne(qa => qa.Quiz)
                .WithMany(q => q.Attempts)
                .HasForeignKey(qa => qa.QuizId);

            builder.Entity<QuizAnswer>()
                .HasOne(qa => qa.QuizAttempt)
                .WithMany(qa => qa.Answers)
                .HasForeignKey(qa => qa.QuizAttemptId);

            builder.Entity<Enrollment>()
                .HasOne(e => e.Student)
                .WithMany(u => u.Enrollments)
                .HasForeignKey(e => e.StudentId);

            builder.Entity<Enrollment>()
                .HasOne(e => e.Course)
                .WithMany(c => c.Enrollments)
                .HasForeignKey(e => e.CourseId);

            builder.Entity<Resource>()
                .HasOne(r => r.Lesson)
                .WithMany(l => l.Resources)
                .HasForeignKey(r => r.LessonId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<LessonCompletion>()
                .HasIndex(c => new { c.StudentId, c.LessonId })
                .IsUnique();
            builder.Entity<LessonCompletion>()
                .HasOne(c => c.Student)
                .WithMany()
                .HasForeignKey(c => c.StudentId)
                .OnDelete(DeleteBehavior.NoAction);
            builder.Entity<LessonCompletion>()
                .HasOne(c => c.Lesson)
                .WithMany()
                .HasForeignKey(c => c.LessonId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<StudentAward>()
                .HasIndex(a => new { a.StudentId, a.AwardKey })
                .IsUnique();
            builder.Entity<StudentAward>()
                .HasOne(a => a.Student)
                .WithMany()
                .HasForeignKey(a => a.StudentId)
                .OnDelete(DeleteBehavior.NoAction);
        }
    }
}
