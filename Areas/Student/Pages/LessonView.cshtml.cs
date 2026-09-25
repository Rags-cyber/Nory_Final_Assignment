using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using NoryMusicLMS_VS.Data;
using NoryMusicLMS_VS.Models;
using NoryMusicLMS_VS.Services;
using Quiz = NoryMusicLMS_VS.Models.Quiz;

namespace NoryMusicLMS_VS.Areas.Student.Pages
{
    [Authorize(Roles = "Student")]
    public class LessonViewModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly AwardService _awardService;

        public LessonViewModel(ApplicationDbContext context, UserManager<ApplicationUser> userManager, AwardService awardService)
        {
            _context = context;
            _userManager = userManager;
            _awardService = awardService;
        }

        public Lesson Lesson { get; set; } = default!;
        public Course Course { get; set; } = default!;
        public Enrollment Enrollment { get; set; } = default!;
        public IList<NoryMusicLMS_VS.Models.Quiz> Quizzes { get; set; } = default!;
        public IList<Resource> Resources { get; set; } = default!;
        public int CurrentLessonIndex { get; set; }
        public int TotalLessons { get; set; }
        public int? PreviousLessonId { get; set; }
        public int? NextLessonId { get; set; }

        public async Task<IActionResult> OnGetAsync(int? lessonId)
        {
            if (lessonId == null)
            {
                return NotFound();
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            // Get the lesson
            Lesson = await _context.Lessons
                .Include(l => l.Course)
                .FirstOrDefaultAsync(l => l.Id == lessonId);

            if (Lesson == null)
            {
                return NotFound();
            }

            Course = Lesson.Course;

            // Check if user is enrolled in this course
            Enrollment = await _context.Enrollments
                .FirstOrDefaultAsync(e => e.StudentId == user.Id && e.CourseId == Course.Id &&
                    e.Status != EnrollmentStatus.Dropped);

            if (Enrollment == null)
            {
                TempData["ErrorMessage"] = "You need to enroll in this course to view lessons.";
                return RedirectToPage("./BrowseCourses");
            }

            // Get all lessons for this course to determine position
            var allLessons = await _context.Lessons
                .Where(l => l.CourseId == Course.Id)
                .OrderBy(l => l.OrderIndex)
                .ToListAsync();

            TotalLessons = allLessons.Count;
            CurrentLessonIndex = allLessons.FindIndex(l => l.Id == lessonId) + 1; // 1-based index
            if (CurrentLessonIndex > 1)
                PreviousLessonId = allLessons[CurrentLessonIndex - 2].Id;
            if (CurrentLessonIndex > 0 && CurrentLessonIndex < TotalLessons)
                NextLessonId = allLessons[CurrentLessonIndex].Id;

            // Get quizzes for this lesson
            Quizzes = await _context.Quizzes
                .Where(q => q.LessonId == lessonId && q.IsActive)
                .OrderBy(q => q.CreatedAt)
                .ToListAsync();

            Resources = await _context.Resources
                .Where(r => r.LessonId == lessonId)
                .OrderBy(r => r.UploadedAt)
                .ToListAsync();

            return Page();
        }

        public async Task<IActionResult> OnPostMarkCompleteAsync(int lessonId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            var lesson = await _context.Lessons
                .AsNoTracking()
                .FirstOrDefaultAsync(l => l.Id == lessonId);
            if (lesson is null)
                return NotFound();

            var enrollment = await _context.Enrollments
                .FirstOrDefaultAsync(e => e.StudentId == user.Id &&
                    e.CourseId == lesson.CourseId && e.Status == EnrollmentStatus.Active);
            if (enrollment is null)
            {
                TempData["ErrorMessage"] = "You need an active enrollment to complete this lesson.";
                return RedirectToPage("./BrowseCourses");
            }

            var alreadyCompleted = await _context.LessonCompletions
                .AnyAsync(c => c.StudentId == user.Id && c.LessonId == lessonId);
            if (!alreadyCompleted)
            {
                _context.LessonCompletions.Add(new LessonCompletion
                {
                    StudentId = user.Id,
                    LessonId = lessonId,
                    CompletedAt = DateTime.UtcNow
                });
            }

            var totalLessons = await _context.Lessons.CountAsync(l => l.CourseId == lesson.CourseId);
            var completedLessons = await _context.LessonCompletions
                .CountAsync(c => c.StudentId == user.Id && c.Lesson.CourseId == lesson.CourseId);
            if (!alreadyCompleted)
                completedLessons++;

            enrollment.ProgressPercentage = totalLessons == 0
                ? 0
                : Math.Round((double)completedLessons / totalLessons * 100, 2);

            if (totalLessons > 0 && completedLessons >= totalLessons)
            {
                enrollment.Status = EnrollmentStatus.Completed;
                enrollment.CompletedAt ??= DateTime.UtcNow;
                enrollment.ProgressPercentage = 100;
            }

            await _context.SaveChangesAsync();
            if (totalLessons > 0 && completedLessons >= totalLessons)
            {
                var courseTitle = await _context.Courses
                    .Where(c => c.Id == lesson.CourseId)
                    .Select(c => c.Title)
                    .FirstAsync();
                await _awardService.GrantOnceAsync(
                    user.Id,
                    $"course-complete-{lesson.CourseId}",
                    $"Course completed: {courseTitle}",
                    $"Completed every lesson in {courseTitle}.");
            }

            return RedirectToPage(new { lessonId });
        }
    }
}
