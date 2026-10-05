using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using NoryMusicLMS_VS.Data;
using NoryMusicLMS_VS.Models;

namespace NoryMusicLMS_VS.Areas.Admin.Pages.Resources
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
        public Resource Resource { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null) return NotFound();

            var resource = await _context.Resources.FindAsync(id);
            if (resource == null) return NotFound();

            Resource = resource;
            ViewData["LessonId"] = new SelectList(_context.Lessons.OrderBy(l => l.Title), "Id", "Title", Resource.LessonId);
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                ViewData["LessonId"] = new SelectList(_context.Lessons.OrderBy(l => l.Title), "Id", "Title", Resource.LessonId);
                return Page();
            }

            var resourceToUpdate = await _context.Resources.FindAsync(Resource.Id);
            if (resourceToUpdate is null) return NotFound();

            if (!await _context.Lessons.AnyAsync(l => l.Id == Resource.LessonId))
            {
                ModelState.AddModelError(nameof(Resource.LessonId), "Select a valid lesson.");
                ViewData["LessonId"] = new SelectList(_context.Lessons.OrderBy(l => l.Title), "Id", "Title", Resource.LessonId);
                return Page();
            }

            if (!await TryUpdateModelAsync(resourceToUpdate, "Resource",
                r => r.Title, r => r.Description, r => r.Type, r => r.Url, r => r.LessonId))
                return Page();

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException)
            {
                if (!_context.Resources.Any(e => e.Id == Resource.Id)) return NotFound();
                throw;
            }

            return RedirectToPage("./Index");
        }
    }
}
