using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using NoryMusicLMS_VS.Data;
using NoryMusicLMS_VS.Models;

namespace NoryMusicLMS_VS.Areas.Instructor.Pages.Lessons;

[Authorize(Roles = "Instructor")]
public class EditModel : PageModel
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _users;

    public EditModel(ApplicationDbContext db, UserManager<ApplicationUser> users)
    {
        _db = db;
        _users = users;
    }

    [BindProperty] public LessonInput Input { get; set; } = new();
    public int LessonId { get; private set; }
    public string CourseTitle { get; private set; } = "";

    public class LessonInput
    {
        [Required, StringLength(100)] public string Title { get; set; } = "";
        [StringLength(2000)] public string Content { get; set; } = "";
        [Range(1, 1000)] public int OrderIndex { get; set; }
        [Url] public string? VideoUrl { get; set; }
        [Url] public string? AudioUrl { get; set; }
        [Url] public string? NotationUrl { get; set; }
    }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var lesson = await GetOwnedLessonAsync(id);
        if (lesson is null) return NotFound();
        LessonId = lesson.Id;
        CourseTitle = lesson.Course.Title;
        Input = new LessonInput
        {
            Title = lesson.Title,
            Content = lesson.Content,
            OrderIndex = lesson.OrderIndex,
            VideoUrl = lesson.VideoUrl,
            AudioUrl = lesson.AudioUrl,
            NotationUrl = lesson.NotationUrl
        };
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int id)
    {
        LessonId = id;
        var lesson = await GetOwnedLessonAsync(id);
        if (lesson is null) return NotFound();
        CourseTitle = lesson.Course.Title;
        if (!ModelState.IsValid) return Page();

        lesson.Title = Input.Title.Trim();
        lesson.Content = Input.Content.Trim();
        lesson.OrderIndex = Input.OrderIndex;
        lesson.VideoUrl = Input.VideoUrl;
        lesson.AudioUrl = Input.AudioUrl;
        lesson.NotationUrl = Input.NotationUrl;
        await _db.SaveChangesAsync();
        TempData["StatusMessage"] = "Lesson updated.";
        return RedirectToPage("/Courses/Details", new { area = "Instructor", id = lesson.CourseId });
    }

    private Task<Lesson?> GetOwnedLessonAsync(int id)
    {
        var userId = _users.GetUserId(User);
        return _db.Lessons.Include(l => l.Course)
            .FirstOrDefaultAsync(l => l.Id == id && l.Course.InstructorId == userId);
    }
}
