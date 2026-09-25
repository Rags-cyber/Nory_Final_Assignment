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
public class CreateModel : PageModel
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _users;

    public CreateModel(ApplicationDbContext db, UserManager<ApplicationUser> users)
    {
        _db = db;
        _users = users;
    }

    [BindProperty] public LessonInput Input { get; set; } = new();
    public string CourseTitle { get; private set; } = "";

    public class LessonInput
    {
        [Required] public int CourseId { get; set; }
        [Required, StringLength(100)] public string Title { get; set; } = "";
        [StringLength(2000)] public string Content { get; set; } = "";
        [Range(1, 1000)] public int OrderIndex { get; set; } = 1;
        [Url] public string? VideoUrl { get; set; }
        [Url] public string? AudioUrl { get; set; }
        [Url] public string? NotationUrl { get; set; }
    }

    public async Task<IActionResult> OnGetAsync(int courseId)
    {
        var course = await GetOwnedCourseAsync(courseId);
        if (course is null) return NotFound();
        Input.CourseId = course.Id;
        Input.OrderIndex = await _db.Lessons.CountAsync(l => l.CourseId == course.Id) + 1;
        CourseTitle = course.Title;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var course = await GetOwnedCourseAsync(Input.CourseId);
        if (course is null) return NotFound();
        CourseTitle = course.Title;
        if (!ModelState.IsValid) return Page();

        _db.Lessons.Add(new Lesson
        {
            CourseId = course.Id,
            Title = Input.Title.Trim(),
            Content = Input.Content.Trim(),
            OrderIndex = Input.OrderIndex,
            VideoUrl = Input.VideoUrl,
            AudioUrl = Input.AudioUrl,
            NotationUrl = Input.NotationUrl,
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();
        TempData["StatusMessage"] = "Lesson created.";
        return RedirectToPage("/Courses/Details", new { area = "Instructor", id = course.Id });
    }

    private Task<Course?> GetOwnedCourseAsync(int courseId)
    {
        var userId = _users.GetUserId(User);
        return _db.Courses.FirstOrDefaultAsync(c => c.Id == courseId && c.InstructorId == userId);
    }
}
