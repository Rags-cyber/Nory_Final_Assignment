using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using NoryMusicLMS_VS.Data;
using NoryMusicLMS_VS.Models;

namespace NoryMusicLMS_VS.Areas.Admin.Pages.Resources
{
    [Authorize(Roles = "Admin")]
    public class CreateModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public CreateModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult OnGet()
        {
            ViewData["LessonId"] = new SelectList(_context.Lessons.OrderBy(l => l.Title), "Id", "Title");
            return Page();
        }

        [BindProperty]
        public Resource Resource { get; set; } = default!;

        public async Task<IActionResult> OnPostAsync()
        {
            if (!await _context.Lessons.AnyAsync(l => l.Id == Resource.LessonId))
                ModelState.AddModelError("Resource.LessonId", "Select a valid lesson.");

            if (!ModelState.IsValid)
            {
                ViewData["LessonId"] = new SelectList(_context.Lessons.OrderBy(l => l.Title), "Id", "Title");
                return Page();
            }

            _context.Resources.Add(Resource);
            await _context.SaveChangesAsync();

            return RedirectToPage("./Index");
        }
    }
}
