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
    public class CourseDetailsModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public CourseDetailsModel(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public Course Course { get; set; } = default!;
        public Enrollment Enrollment { get; set; } = default!;
        public IList<Lesson> Lessons { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            // Get the course
            Course = await _context.Courses
                .Include(c => c.Instructor)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (Course == null)
            {
                return NotFound();
            }

            // Get the enrollment for this user and course
            Enrollment = await _context.Enrollments
                .FirstOrDefaultAsync(e => e.StudentId == user.Id && e.CourseId == id);

            if (Enrollment == null)
            {
                // Not enrolled - redirect to browse with message
                TempData["ErrorMessage"] = "You need to enroll in this course to view it.";
                return RedirectToPage("./BrowseCourses");
            }

            // Get lessons for this course in order
            Lessons = await _context.Lessons
                .Where(l => l.CourseId == id)
                .OrderBy(l => l.OrderIndex)
                .ToListAsync();

            return Page();
        }
    }
}