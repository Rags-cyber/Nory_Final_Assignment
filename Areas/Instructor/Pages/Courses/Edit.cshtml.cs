using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using NoryMusicLMS_VS.Data;
using NoryMusicLMS_VS.Models;
using NoryMusicLMS_VS.Services;

namespace NoryMusicLMS_VS.Areas.Instructor.Pages.Courses;

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

    [BindProperty] public CourseInput Input { get; set; } = new();
    public int CourseId { get; private set; }
    public string? CurrentImageUrl { get; private set; }

    public class CourseInput
    {
        [Required, StringLength(100)] public string Title { get; set; } = "";
        [StringLength(500)] public string Description { get; set; } = "";
        [DataType(DataType.Date)] public DateTime StartDate { get; set; }
        [DataType(DataType.Date)] public DateTime EndDate { get; set; }
        public bool IsActive { get; set; }
        public IFormFile? Image { get; set; }
        public bool RemoveImage { get; set; }
    }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var course = await GetOwnedCourseAsync(id);
        if (course is null) return NotFound();
        CourseId = course.Id;
        CurrentImageUrl = course.ImageUrl;
        Input = new CourseInput
        {
            Title = course.Title,
            Description = course.Description,
            StartDate = course.StartDate,
            EndDate = course.EndDate,
            IsActive = course.IsActive
        };
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int id)
    {
        CourseId = id;
        var course = await GetOwnedCourseAsync(id);
        if (course is null) return NotFound();
        CurrentImageUrl = course.ImageUrl;
        if (ImageStorageService.Validate(Input.Image) is { } imageError)
            ModelState.AddModelError("Input.Image", imageError);
        if (Input.EndDate < Input.StartDate)
            ModelState.AddModelError("Input.EndDate", "End date must be on or after the start date.");
        if (!ModelState.IsValid) return Page();

        course.Title = Input.Title.Trim();
        course.Description = Input.Description.Trim();
        course.StartDate = Input.StartDate;
        course.EndDate = Input.EndDate;
        course.IsActive = Input.IsActive;
        course.ImageUrl = await _images.ApplyAsync(course.ImageUrl, Input.Image, Input.RemoveImage, _users.GetUserId(User));
        await _db.SaveChangesAsync();
        TempData["StatusMessage"] = "Course updated.";
        return RedirectToPage("./Details", new { id });
    }

    private Task<Course?> GetOwnedCourseAsync(int id)
    {
        var userId = _users.GetUserId(User);
        return _db.Courses.FirstOrDefaultAsync(c => c.Id == id && c.InstructorId == userId);
    }
}
