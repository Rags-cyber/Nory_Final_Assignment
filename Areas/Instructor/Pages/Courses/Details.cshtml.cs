using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using NoryMusicLMS_VS.Data;
using NoryMusicLMS_VS.Models;

namespace NoryMusicLMS_VS.Areas.Instructor.Pages.Courses;

[Authorize(Roles = "Instructor")]
public class DetailsModel : PageModel
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _users;

    public DetailsModel(ApplicationDbContext db, UserManager<ApplicationUser> users)
    {
        _db = db;
        _users = users;
    }

    public Course Course { get; private set; } = default!;
    public IList<Enrollment> Enrollments { get; private set; } = new List<Enrollment>();

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var userId = _users.GetUserId(User);
        var course = await _db.Courses.AsNoTracking()
            .Include(c => c.Lessons.OrderBy(l => l.OrderIndex))
                .ThenInclude(l => l.Quizzes)
            .Include(c => c.Lessons)
                .ThenInclude(l => l.Resources)
            .FirstOrDefaultAsync(c => c.Id == id && c.InstructorId == userId);
        if (course is null) return NotFound();
        Course = course;
        Enrollments = await _db.Enrollments.AsNoTracking()
            .Include(e => e.Student)
            .Where(e => e.CourseId == course.Id)
            .OrderByDescending(e => e.ProgressPercentage)
            .ToListAsync();
        return Page();
    }
}
