using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using NoryMusicLMS_VS.Data;
using NoryMusicLMS_VS.Models;

namespace NoryMusicLMS_VS.Areas.Student.Pages
{
    [Authorize(Roles = "Student")]
    public class BrowseCoursesModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public BrowseCoursesModel(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public IList<Course> Courses { get; set; } = default!;
        public IList<string> EnrolledCourseIds { get; set; } = default!;

        public async Task OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user != null)
            {
                
                EnrolledCourseIds = await _context.Enrollments
                    .Where(e => e.StudentId == user.Id && e.Status != EnrollmentStatus.Dropped)
                    .Select(e => e.CourseId.ToString())
                    .ToListAsync();

                
                Courses = await _context.Courses
                    .Where(c => c.IsActive && !EnrolledCourseIds.Contains(c.Id.ToString()))
                    .Include(c => c.Instructor)
                    .Include(c => c.Lessons)
                    .OrderBy(c => c.Title)
                    .ToListAsync();
            }
        }

        public async Task<IActionResult> OnPostEnrollAsync(int courseId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            if (!await _context.Courses.AnyAsync(c => c.Id == courseId && c.IsActive))
            {
                return NotFound();
            }

            
            var existingEnrollment = await _context.Enrollments
                .FirstOrDefaultAsync(e => e.StudentId == user.Id && e.CourseId == courseId);

            if (existingEnrollment != null)
            {
                
                if (existingEnrollment.Status == EnrollmentStatus.Dropped)
                {
                    existingEnrollment.Status = EnrollmentStatus.Active;
                    existingEnrollment.EnrolledAt = DateTime.UtcNow;
                    var lessonCount = await _context.Lessons
                        .CountAsync(l => l.CourseId == courseId);
                    var completedCount = await _context.LessonCompletions
                        .CountAsync(c => c.StudentId == user.Id && c.Lesson.CourseId == courseId);
                    existingEnrollment.ProgressPercentage = lessonCount == 0
                        ? 0
                        : Math.Round((double)completedCount / lessonCount * 100, 2);
                    await _context.SaveChangesAsync();
                }
                return RedirectToPage("./Index");
            }

            
            var enrollment = new Enrollment
            {
                StudentId = user.Id,
                CourseId = courseId,
                EnrolledAt = DateTime.UtcNow,
                Status = EnrollmentStatus.Active,
                ProgressPercentage = 0
            };

            _context.Enrollments.Add(enrollment);
            await _context.SaveChangesAsync();

            return RedirectToPage("./Index");
        }

        public async Task<IActionResult> OnPostDropAsync(int courseId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            var enrollment = await _context.Enrollments
                .FirstOrDefaultAsync(e => e.StudentId == user.Id && e.CourseId == courseId);

            if (enrollment != null)
            {
                enrollment.Status = EnrollmentStatus.Dropped;
                _context.Enrollments.Update(enrollment);
                await _context.SaveChangesAsync();
            }

            return RedirectToPage("./Index");
        }
    }
}
