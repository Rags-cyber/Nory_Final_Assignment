using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using NoryMusicLMS_VS.Data;
using NoryMusicLMS_VS.Models;
using NoryMusicLMS_VS.Services;

namespace NoryMusicLMS_VS.Areas.Instructor.Pages.Quizzes;

[Authorize(Roles = "Instructor")]
public class CreateModel : PageModel
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _users;
    private readonly ImageStorageService _images;

    public CreateModel(ApplicationDbContext db, UserManager<ApplicationUser> users, ImageStorageService images)
    {
        _db = db;
        _users = users;
        _images = images;
    }

    [BindProperty] public QuizInput Input { get; set; } = new();
    public string LessonTitle { get; private set; } = "";

    public class QuizInput
    {
        [Required, StringLength(100)] public string Title { get; set; } = "";
        [StringLength(500)] public string Description { get; set; } = "";
        public QuizType Type { get; set; }
        public IFormFile? Image { get; set; }
    }

    public async Task<IActionResult> OnGetAsync(int lessonId)
    {
        var lesson = await GetOwnedLessonAsync(lessonId);
        if (lesson is null) return NotFound();
        LessonTitle = lesson.Title;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int lessonId)
    {
        var lesson = await GetOwnedLessonAsync(lessonId);
        if (lesson is null) return NotFound();
        LessonTitle = lesson.Title;
        if (ImageStorageService.Validate(Input.Image) is { } imageError)
            ModelState.AddModelError("Input.Image", imageError);
        if (!ModelState.IsValid) return Page();

        var imageUrl = await _images.ApplyAsync(null, Input.Image, false, _users.GetUserId(User));

        var quiz = new Quiz
        {
            LessonId = lesson.Id,
            Title = Input.Title.Trim(),
            Description = Input.Description.Trim(),
            Type = Input.Type,
            IsActive = true,
            ImageUrl = imageUrl,
            CreatedAt = DateTime.UtcNow
        };
        _db.Quizzes.Add(quiz);
        await _db.SaveChangesAsync();
        TempData["StatusMessage"] = "Quiz created. Add questions to make it ready for students.";
        return RedirectToPage("./Manage", new { id = quiz.Id });
    }

    private Task<Lesson?> GetOwnedLessonAsync(int lessonId)
    {
        var userId = _users.GetUserId(User);
        return _db.Lessons.Include(l => l.Course)
            .FirstOrDefaultAsync(l => l.Id == lessonId && l.Course.InstructorId == userId);
    }
}
