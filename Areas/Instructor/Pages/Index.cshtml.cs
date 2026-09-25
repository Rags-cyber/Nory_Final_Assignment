using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using NoryMusicLMS_VS.Data;
using NoryMusicLMS_VS.Models;

namespace NoryMusicLMS_VS.Areas.Instructor.Pages
{
    [Authorize(Roles = "Instructor")]
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public IndexModel(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public IList<Course> Courses { get; set; } = default!;
        public int TotalStudents { get; set; }
        public int TotalLessons { get; set; }
        public int TotalQuizzes { get; set; }

        public async Task OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                RedirectToPage("/Index");
                return;
            }

            // Get courses taught by this instructor
            Courses = await _context.Courses
                .Where(c => c.InstructorId == user.Id)
                .Include(c => c.Lessons)
                .Include(c => c.Enrollments)
                .ToListAsync();

            // Count enrolled students (distinct from all instructor's courses)
            var enrolledStudentIds = await _context.Enrollments
                .Where(e => e.Status != EnrollmentStatus.Dropped)
                .Where(e => Courses.Select(c => c.Id).Contains(e.CourseId))
                .Select(e => e.StudentId)
                .Distinct()
                .CountAsync();

            TotalStudents = enrolledStudentIds;

            // Count lessons and quizzes in instructor's courses
            TotalLessons = await _context.Lessons
                .Where(l => Courses.Select(c => c.Id).Contains(l.CourseId))
                .CountAsync();

            var instructorCourseIds = Courses.Select(c => c.Id).ToList();
            var instructorLessonIds = await _context.Lessons
                .Where(l => instructorCourseIds.Contains(l.CourseId))
                .Select(l => l.Id)
                .ToListAsync();
            TotalQuizzes = await _context.Quizzes
                .Where(q => instructorLessonIds.Contains(q.LessonId))
                .CountAsync();
        }
    }
}
