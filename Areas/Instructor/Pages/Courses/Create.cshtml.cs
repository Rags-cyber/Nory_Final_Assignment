using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using NoryMusicLMS_VS.Data;
using NoryMusicLMS_VS.Models;
using NoryMusicLMS_VS.Services;

namespace NoryMusicLMS_VS.Areas.Instructor.Pages.Courses;

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

    [BindProperty] public CourseInput Input { get; set; } = new();

    public class CourseInput
    {
        [Required, StringLength(100)] public string Title { get; set; } = "";
        [StringLength(500)] public string Description { get; set; } = "";
        [DataType(DataType.Date)] public DateTime StartDate { get; set; } = DateTime.Today;
        [DataType(DataType.Date)] public DateTime EndDate { get; set; } = DateTime.Today.AddMonths(3);
        public bool IsActive { get; set; } = true;
        public IFormFile? Image { get; set; }
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (Input.EndDate < Input.StartDate)
            ModelState.AddModelError("Input.EndDate", "End date must be on or after the start date.");
        if (ImageStorageService.Validate(Input.Image) is { } imageError)
            ModelState.AddModelError("Input.Image", imageError);
        if (!ModelState.IsValid) return Page();

        var instructorId = _users.GetUserId(User);
        if (instructorId is null) return Forbid();

        var imageUrl = await _images.ApplyAsync(null, Input.Image, false, instructorId);

        _db.Courses.Add(new Course
        {
            Title = Input.Title.Trim(),
            Description = Input.Description.Trim(),
            InstructorId = instructorId,
            StartDate = Input.StartDate,
            EndDate = Input.EndDate,
            IsActive = Input.IsActive,
            ImageUrl = imageUrl,
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();
        TempData["StatusMessage"] = "Course created.";
        return RedirectToPage("./Index");
    }
}
