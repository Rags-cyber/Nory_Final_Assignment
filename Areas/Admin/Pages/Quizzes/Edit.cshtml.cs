using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using NoryMusicLMS_VS.Data;
using NoryMusicLMS_VS.Models;

namespace NoryMusicLMS_VS.Areas.Admin.Pages.Quizzes
{
    [Authorize(Roles = "Admin")]
    public class EditModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public EditModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult OnGet(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            Quiz = _context.Quizzes
                .Include(q => q.Lesson)
                .FirstOrDefault(q => q.Id == id);

            if (Quiz == null)
            {
                return NotFound();
            }

            // Populate lesson dropdown
            ViewData["LessonId"] = new SelectList(_context.Lessons
                .Include(l => l.Course)
                .Where(l => l.Course.IsActive), "Id", "Title", Quiz.LessonId);
            return Page();
        }

        [BindProperty]
        public Quiz Quiz { get; set; } = default!;

        public async Task<IActionResult> OnPostAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var quizToUpdate = await _context.Quizzes.FindAsync(id);

            if (quizToUpdate == null)
            {
                return NotFound();
            }

            if (await TryUpdateModelAsync<Quiz>(
                quizToUpdate,
                "Quiz",
                q => q.Title, q => q.Description, q => q.Type, q => q.IsActive))
            {
                try
                {
                    await _context.SaveChangesAsync();
                    return RedirectToPage("./Index");
                }
                catch (DbUpdateException /* ex */)
                {
                    ModelState.AddModelError("", "Unable to save changes. " +
                        "Try again, and if the problem persists, " +
                        "see your system administrator.");
                }
            }

            ViewData["LessonId"] = new SelectList(_context.Lessons
                .Include(l => l.Course)
                .Where(l => l.Course.IsActive), "Id", "Title", quizToUpdate.LessonId);
            return Page();
        }
    }
}