using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using NoryMusicLMS_VS.Data;
using NoryMusicLMS_VS.Models;
using NoryMusicLMS_VS.Services;

namespace NoryMusicLMS_VS.Areas.Instructor.Pages.Lessons;

[Authorize(Roles = "Instructor")]
public class EditModel : PageModel
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _users;
    private readonly ImageStorageService _images;

    public EditModel(ApplicationDbContext db, UserManager<ApplicationUser> users, ImageStorageService images)
    {
        _db = db;
        _users = users;
        _images = images;
    }

    [BindProperty] public LessonInput Input { get; set; } = new();
    public int LessonId { get; private set; }
    public string CourseTitle { get; private set; } = "";
    public string? CurrentImageUrl { get; private set; }

    public class LessonInput
    {
        [Required, StringLength(100)] public string Title { get; set; } = "";
        [StringLength(2000)] public string Content { get; set; } = "";
        [Range(1, 1000)] public int OrderIndex { get; set; }
        [Url] public string? VideoUrl { get; set; }
        [Url] public string? AudioUrl { get; set; }
        [Url] public string? NotationUrl { get; set; }
        public IFormFile? Image { get; set; }
        public bool RemoveImage { get; set; }
    }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var lesson = await GetOwnedLessonAsync(id);
        if (lesson is null) return NotFound();
        LessonId = lesson.Id;
        CourseTitle = lesson.Course.Title;
        CurrentImageUrl = lesson.ImageUrl;
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
        CurrentImageUrl = lesson.ImageUrl;
        if (ImageStorageService.Validate(Input.Image) is { } imageError)
            ModelState.AddModelError("Input.Image", imageError);
        if (!ModelState.IsValid) return Page();

        lesson.Title = Input.Title.Trim();
        lesson.Content = Input.Content.Trim();
        lesson.OrderIndex = Input.OrderIndex;
        lesson.VideoUrl = Input.VideoUrl;
        lesson.AudioUrl = Input.AudioUrl;
        lesson.NotationUrl = Input.NotationUrl;
        lesson.ImageUrl = await _images.ApplyAsync(lesson.ImageUrl, Input.Image, Input.RemoveImage, _users.GetUserId(User));
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
