using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using NoryMusicLMS_VS.Data;
using NoryMusicLMS_VS.Models;

namespace NoryMusicLMS_VS.Areas.Admin.Pages.Lessons
{
    [Authorize(Roles = "Admin")]
    public class DeleteModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public DeleteModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public Lesson Lesson { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            Lesson = await _context.Lessons
                .Include(l => l.Course)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (Lesson == null)
            {
                return NotFound();
            }
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var lesson = await _context.Lessons
                .Include(l => l.Quizzes)
                .FirstOrDefaultAsync(l => l.Id == id.Value);

            if (lesson == null)
            {
                return NotFound();
            }

            
            
            foreach (var quiz in lesson.Quizzes.ToList())
            {
                var questions = await _context.QuizQuestions
                    .Where(q => q.QuizId == quiz.Id)
                    .ToListAsync();
                foreach (var q in questions)
                {
                    _context.QuizQuestions.Remove(q);
                }
            }

            _context.Lessons.Remove(lesson);
            await _context.SaveChangesAsync();

            TempData["StatusMessage"] = $"Lesson \"{lesson.Title}\" deleted.";
            return RedirectToPage("./Index");
        }
    }
}