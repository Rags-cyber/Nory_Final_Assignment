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
    public class CreateModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public CreateModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult OnGet()
        {
            // Populate quiz dropdown
            ViewData["QuizId"] = new SelectList(_context.Quizzes
                .Include(q => q.Lesson)
                    .ThenInclude(l => l.Course), "Id", "Title");
            return Page();
        }

        [BindProperty]
        public QuizQuestion QuizQuestion { get; set; } = default!;

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                await PopulateQuizzesAsync();
                return Page();
            }

            // Handle different question types
            switch (QuizQuestion.Type)
            {
                case QuestionType.MultipleChoice:
                case QuestionType.TrueFalse:
                    var options = new List<string>();
                    if (QuizQuestion.Type == QuestionType.TrueFalse)
                    {
                        options.AddRange(["True", "False"]);
                    }
                    else
                    {
                        for (int i = 1; i <= 4; i++)
                        {
                            var option = Request.Form[$"Option{i}"].ToString().Trim();
                            if (!string.IsNullOrEmpty(option))
                                options.Add(option);
                        }
                    }

                    if (options.Count < 2 ||
                        !int.TryParse(Request.Form["CorrectAnswerIndex"], out var displayedIndex) ||
                        displayedIndex < 1 || displayedIndex > options.Count)
                    {
                        ModelState.AddModelError(string.Empty, "Provide at least two options and choose a valid correct option.");
                        await PopulateQuizzesAsync();
                        return Page();
                    }

                    QuizQuestion.OptionsJson = JsonSerializer.Serialize(options);
                    // The UI asks for option number 1..N; persisted indexes are zero-based.
                    QuizQuestion.CorrectAnswerIndex = displayedIndex - 1;
                    break;

                case QuestionType.FillInBlank:
                case QuestionType.AudioIdentification:
                    QuizQuestion.CorrectAnswerText = Request.Form["CorrectAnswerText"].ToString().Trim();
                    if (string.IsNullOrWhiteSpace(QuizQuestion.CorrectAnswerText))
                    {
                        ModelState.AddModelError(string.Empty, "Enter the expected answer.");
                        await PopulateQuizzesAsync();
                        return Page();
                    }
                    if (QuizQuestion.Type == QuestionType.AudioIdentification &&
                        string.IsNullOrWhiteSpace(QuizQuestion.AudioUrl) &&
                        string.IsNullOrWhiteSpace(QuizQuestion.ToneSequence))
                    {
                        ModelState.AddModelError(string.Empty, "Provide an audio URL or a synthesized tone sequence.");
                        await PopulateQuizzesAsync();
                        return Page();
                    }
                    break;
            }

            _context.QuizQuestions.Add(QuizQuestion);
            await _context.SaveChangesAsync();

            return RedirectToPage("./Index");
        }

        private async Task PopulateQuizzesAsync()
        {
            ViewData["QuizId"] = new SelectList(await _context.Quizzes
                .Include(q => q.Lesson)
                .ThenInclude(l => l.Course)
                .OrderBy(q => q.Title)
                .ToListAsync(), "Id", "Title", QuizQuestion?.QuizId);
        }
    }
}
