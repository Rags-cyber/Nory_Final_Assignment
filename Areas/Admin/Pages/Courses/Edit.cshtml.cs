using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using NoryMusicLMS_VS.Data;
using NoryMusicLMS_VS.Models;

namespace NoryMusicLMS_VS.Areas.Admin.Pages.Courses
{
    [Authorize(Roles = "Admin")]
    public class EditModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public EditModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public Course Course { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int id)
        {
            if (id == null)
            {
                return NotFound();
            }

            Course = await _context.Courses
                .Include(c => c.Instructor)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (Course == null)
            {
                return NotFound();
            }

            // Populate instructor dropdown
            ViewData["InstructorId"] = new SelectList(
                _context.Users.Where(u =>
                    _context.UserRoles.Any(ur => ur.UserId == u.Id &&
                    _context.Roles.Any(r => r.Id == ur.RoleId && r.Name == "Instructor"))
                ).Select(u => new {
                    u.Id,
                    FullName = u.FirstName + " " + u.LastName
                }),
                "Id",
                "FullName",
                Course.InstructorId);
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int id)
        {
            if (!ModelState.IsValid)
            {
                // Repopulate dropdown on validation error
                ViewData["InstructorId"] = new SelectList(
                    _context.Users.Where(u =>
                        _context.UserRoles.Any(ur => ur.UserId == u.Id &&
                        _context.Roles.Any(r => r.Id == ur.RoleId && r.Name == "Instructor"))
                    ).Select(u => new {
                        u.Id,
                        FullName = u.FirstName + " " + u.LastName
                    }),
                    "Id",
                    "FullName",
                    Course.InstructorId);
                return Page();
            }

            var courseToUpdate = await _context.Courses.FindAsync(id);

            if (courseToUpdate == null)
            {
                return NotFound();
            }

            if (await TryUpdateModelAsync<Course>(
                courseToUpdate,
                "Course",
                c => c.Title, c => c.Description, c => c.InstructorId,
                c => c.StartDate, c => c.EndDate, c => c.IsActive, c => c.ImageUrl))
            {
                try
                {
                    await _context.SaveChangesAsync();
                    return RedirectToPage("./Index");
                }
                catch (DbUpdateException /* ex */)
                {
                    //Log the error (uncomment ex variable name and write a log.)
                    ModelState.AddModelError("", "Unable to save changes. " +
                        "Try again, and if the problem persists, " +
                        "see your system administrator.");
                }
            }

            // Repopulate dropdown on validation error
            ViewData["InstructorId"] = new SelectList(
                _context.Users.Where(u =>
                    _context.UserRoles.Any(ur => ur.UserId == u.Id &&
                    _context.Roles.Any(r => r.Id == ur.RoleId && r.Name == "Instructor"))
                ).Select(u => new {
                    u.Id,
                    FullName = u.FirstName + " " + u.LastName
                }),
                "Id",
                "FullName",
                Course.InstructorId);
            return Page();
        }
    }
}