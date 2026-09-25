using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using NoryMusicLMS_VS.Data;
using NoryMusicLMS_VS.Models;
using System.Text.Json;

namespace NoryMusicLMS_VS.Areas.Admin.Pages.QuizQuestions
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

            QuizQuestion = _context.QuizQuestions
                .Include(q => q.Quiz)
                    .ThenInclude(q => q.Lesson)
                        .ThenInclude(l => l.Course)
                .FirstOrDefault(q => q.Id == id);

            if (QuizQuestion == null)
            {
                return NotFound();
            }

            // Populate quiz dropdown
            ViewData["QuizId"] = new SelectList(_context.Quizzes
                .Include(q => q.Lesson)
                    .ThenInclude(l => l.Course), "Id", "Title", QuizQuestion.QuizId);
            return Page();
        }

        [BindProperty]
        public QuizQuestion QuizQuestion { get; set; } = default!;

        public async Task<IActionResult> OnPostAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var questionToUpdate = await _context.QuizQuestions.FindAsync(id);

            if (questionToUpdate == null)
            {
                return NotFound();
            }

            if (await TryUpdateModelAsync<QuizQuestion>(
                questionToUpdate,
                "QuizQuestion",
                q => q.QuestionText, q => q.Type, q => q.Points, q => q.OrderIndex, q => q.AudioUrl))
            {
                // Handle different question types for updates
                switch (questionToUpdate.Type)
                {
                    case QuestionType.MultipleChoice:
                    case QuestionType.TrueFalse:
                        // Options as JSON array
                        var options = new List<string>();
                        for (int i = 1; i <= 4; i++)
                        {
                            var option = Request.Form[$"Option{i}"].ToString();
                            if (!string.IsNullOrEmpty(option))
                                options.Add(option);
                        }
                        questionToUpdate.OptionsJson = JsonSerializer.Serialize(options);

                        // Correct answer index
                        if (int.TryParse(Request.Form["CorrectAnswerIndex"], out int correctIndex))
                            questionToUpdate.CorrectAnswerIndex = correctIndex;
                        break;

                    case QuestionType.FillInBlank:
                        questionToUpdate.CorrectAnswerText = Request.Form["CorrectAnswerText"].ToString();
                        break;

                    case QuestionType.AudioIdentification:
                        // For audio identification, the correct answer is the note name
                        questionToUpdate.CorrectAnswerText = Request.Form["CorrectAnswerText"].ToString();
                        break;
                }

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

            // Repopulate quiz dropdown
            ViewData["QuizId"] = new SelectList(_context.Quizzes
                .Include(q => q.Lesson)
                    .ThenInclude(l => l.Course), "Id", "Title", questionToUpdate.QuizId);
            return Page();
        }
    }
}