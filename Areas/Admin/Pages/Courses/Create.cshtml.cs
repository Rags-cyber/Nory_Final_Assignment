using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using NoryMusicLMS_VS.Data;
using NoryMusicLMS_VS.Models;
using NoryMusicLMS_VS.Services;

namespace NoryMusicLMS_VS.Areas.Admin.Pages.Courses;

[Authorize(Roles = "Admin")]
public class CreateModel : PageModel
{
    private readonly ApplicationDbContext _context;
    private readonly ImageStorageService _images;
    public CreateModel(ApplicationDbContext context, ImageStorageService images) { _context = context; _images = images; }

    [BindProperty] public CourseInput Input { get; set; } = new();
    public class CourseInput
    {
        [System.ComponentModel.DataAnnotations.Required, System.ComponentModel.DataAnnotations.StringLength(100)] public string Title { get; set; } = "";
        [System.ComponentModel.DataAnnotations.StringLength(500)] public string Description { get; set; } = "";
        [System.ComponentModel.DataAnnotations.Required] public string InstructorId { get; set; } = "";
        [System.ComponentModel.DataAnnotations.DataType(System.ComponentModel.DataAnnotations.DataType.Date)] public DateTime StartDate { get; set; } = DateTime.Today;
        [System.ComponentModel.DataAnnotations.DataType(System.ComponentModel.DataAnnotations.DataType.Date)] public DateTime EndDate { get; set; } = DateTime.Today.AddMonths(3);
        public bool IsActive { get; set; } = true;
        public IFormFile? Image { get; set; }
    }

    public async Task<IActionResult> OnGetAsync() { await LoadInstructorsAsync(); return Page(); }

    public async Task<IActionResult> OnPostAsync()
    {
        if (Input.EndDate < Input.StartDate) ModelState.AddModelError("Input.EndDate", "End date must be on or after the start date.");
        if (!await IsInstructorAsync(Input.InstructorId)) ModelState.AddModelError("Input.InstructorId", "Select a valid instructor.");
        if (ImageStorageService.Validate(Input.Image) is { } imageError) ModelState.AddModelError("Input.Image", imageError);
        if (!ModelState.IsValid) { await LoadInstructorsAsync(); return Page(); }

        var imageUrl = await _images.ApplyAsync(null, Input.Image, false, null);
        _context.Courses.Add(new Course { Title=Input.Title.Trim(), Description=Input.Description.Trim(), InstructorId=Input.InstructorId, StartDate=Input.StartDate, EndDate=Input.EndDate, IsActive=Input.IsActive, ImageUrl=imageUrl, CreatedAt=DateTime.UtcNow });
        await _context.SaveChangesAsync();
        TempData["StatusMessage"] = "Course created.";
        return RedirectToPage("./Index");
    }

    private async Task<bool> IsInstructorAsync(string? id) => !string.IsNullOrWhiteSpace(id) && await _context.UserRoles.AnyAsync(ur => ur.UserId == id && _context.Roles.Any(r => r.Id == ur.RoleId && r.Name == "Instructor"));
    private async Task LoadInstructorsAsync() => ViewData["InstructorId"] = new SelectList(await _context.Users.Where(u => _context.UserRoles.Any(ur => ur.UserId == u.Id && _context.Roles.Any(r => r.Id == ur.RoleId && r.Name == "Instructor"))).Select(u => new { u.Id, FullName = u.FirstName + " " + u.LastName }).ToListAsync(), "Id", "FullName", Input.InstructorId);
}
