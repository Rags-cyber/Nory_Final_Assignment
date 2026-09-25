using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using NoryMusicLMS_VS.Data;
using NoryMusicLMS_VS.Models;

namespace NoryMusicLMS_VS.Areas.Instructor.Pages.Courses;

[Authorize(Roles = "Instructor")]
public class IndexModel : PageModel
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _users;

    public IndexModel(ApplicationDbContext db, UserManager<ApplicationUser> users)
    {
        _db = db;
        _users = users;
    }

    public IList<Course> Courses { get; private set; } = new List<Course>();

    public async Task OnGetAsync()
    {
        var userId = _users.GetUserId(User);
        Courses = await _db.Courses.AsNoTracking()
            .Where(c => c.InstructorId == userId)
            .Include(c => c.Lessons)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();
    }
}
