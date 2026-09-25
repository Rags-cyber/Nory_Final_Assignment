using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using NoryMusicLMS_VS.Data;
using NoryMusicLMS_VS.Models;

namespace NoryMusicLMS_VS.Areas.Instructor.Pages.Courses;

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

    [BindProperty] public CourseInput Input { get; set; } = new();

    public class CourseInput
    {
        [Required, StringLength(100)] public string Title { get; set; } = "";
        [StringLength(500)] public string Description { get; set; } = "";
        [DataType(DataType.Date)] public DateTime StartDate { get; set; } = DateTime.Today;
        [DataType(DataType.Date)] public DateTime EndDate { get; set; } = DateTime.Today.AddMonths(3);
        public bool IsActive { get; set; } = true;
        [Url] public string? ImageUrl { get; set; }
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (Input.EndDate < Input.StartDate)
            ModelState.AddModelError("Input.EndDate", "End date must be on or after the start date.");
        if (!ModelState.IsValid) return Page();

        var instructorId = _users.GetUserId(User);
        if (instructorId is null) return Forbid();

        _db.Courses.Add(new Course
        {
            Title = Input.Title.Trim(),
            Description = Input.Description.Trim(),
            InstructorId = instructorId,
            StartDate = Input.StartDate,
            EndDate = Input.EndDate,
            IsActive = Input.IsActive,
            ImageUrl = Input.ImageUrl,
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();
        TempData["StatusMessage"] = "Course created.";
        return RedirectToPage("./Index");
    }
}
