using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using NoryMusicLMS_VS.Data;
using NoryMusicLMS_VS.Models;

namespace NoryMusicLMS_VS.Areas.Instructor.Pages.Resources;

[Authorize(Roles = "Instructor")]
public class ManageModel : PageModel
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _users;

    public ManageModel(ApplicationDbContext db, UserManager<ApplicationUser> users)
    {
        _db = db;
        _users = users;
    }

    public Lesson Lesson { get; private set; } = default!;
    [BindProperty] public ResourceInput Input { get; set; } = new();

    public class ResourceInput
    {
        [Required, StringLength(150)] public string Title { get; set; } = "";
        [StringLength(500)] public string? Description { get; set; }
        public ResourceType Type { get; set; }
        [Required, StringLength(1000), Url] public string Url { get; set; } = "";
    }

    public async Task<IActionResult> OnGetAsync(int lessonId)
    {
        var lesson = await GetOwnedLessonAsync(lessonId);
        if (lesson is null) return NotFound();
        Lesson = lesson;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int lessonId)
    {
        var lesson = await GetOwnedLessonAsync(lessonId);
        if (lesson is null) return NotFound();
        Lesson = lesson;
        if (!ModelState.IsValid) return Page();

        _db.Resources.Add(new Resource
        {
            LessonId = lesson.Id,
            Title = Input.Title.Trim(),
            Description = Input.Description?.Trim(),
            Type = Input.Type,
            Url = Input.Url.Trim()
        });
        await _db.SaveChangesAsync();
        TempData["StatusMessage"] = "Resource added.";
        return RedirectToPage(new { lessonId });
    }

    public async Task<IActionResult> OnPostDeleteAsync(int lessonId, int resourceId)
    {
        var lesson = await GetOwnedLessonAsync(lessonId);
        if (lesson is null) return NotFound();
        var resource = await _db.Resources.FirstOrDefaultAsync(r =>
            r.Id == resourceId && r.LessonId == lessonId);
        if (resource is null) return NotFound();
        _db.Resources.Remove(resource);
        await _db.SaveChangesAsync();
        TempData["StatusMessage"] = "Resource deleted.";
        return RedirectToPage(new { lessonId });
    }

    private Task<Lesson?> GetOwnedLessonAsync(int lessonId)
    {
        var userId = _users.GetUserId(User);
        return _db.Lessons
            .Include(l => l.Course)
            .Include(l => l.Resources.OrderBy(r => r.Title))
            .FirstOrDefaultAsync(l => l.Id == lessonId && l.Course.InstructorId == userId);
    }
}
