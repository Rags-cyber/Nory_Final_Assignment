using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using NoryMusicLMS_VS.Data;
using NoryMusicLMS_VS.Models;

namespace NoryMusicLMS_VS.Areas.Student.Pages;

[Authorize(Roles = "Student")]
public class QuizzesModel : PageModel
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _users;

    public QuizzesModel(ApplicationDbContext db, UserManager<ApplicationUser> users)
    {
        _db = db;
        _users = users;
    }

    public IList<NoryMusicLMS_VS.Models.Quiz> Quizzes { get; private set; } =
        new List<NoryMusicLMS_VS.Models.Quiz>();
    [BindProperty(SupportsGet = true)]
    public QuizType? Type { get; set; }

    public async Task OnGetAsync()
    {
        var studentId = _users.GetUserId(User);
        var enrolledCourseIds = _db.Enrollments
            .Where(e => e.StudentId == studentId && e.Status == EnrollmentStatus.Active)
            .Select(e => e.CourseId);

        var query = _db.Quizzes
            .Include(q => q.Lesson)
                .ThenInclude(l => l.Course)
            .Where(q => q.IsActive && enrolledCourseIds.Contains(q.Lesson.CourseId));

        if (Type.HasValue)
            query = query.Where(q => q.Type == Type.Value);

        Quizzes = await query
            .OrderBy(q => q.Lesson.Course.Title)
            .ThenBy(q => q.Lesson.OrderIndex)
            .ThenBy(q => q.Title)
            .ToListAsync();
    }
}
