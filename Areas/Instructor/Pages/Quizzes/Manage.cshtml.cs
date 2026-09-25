using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using NoryMusicLMS_VS.Data;
using NoryMusicLMS_VS.Models;

namespace NoryMusicLMS_VS.Areas.Instructor.Pages.Quizzes;

[Authorize(Roles = "Instructor")]
public class ManageModel : PageModel
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _users;

    public ManageModel(ApplicationDbContext db, UserManager<ApplicationUser> users)
    {
        _db = db;
        _users = users;
    }

    public NoryMusicLMS_VS.Models.Quiz Quiz { get; private set; } = default!;
    [BindProperty] public QuestionInput Input { get; set; } = new();

    public class QuestionInput
    {
        [Required, StringLength(500)] public string QuestionText { get; set; } = "";
        public QuestionType Type { get; set; }
        [Range(1, 100)] public int Points { get; set; } = 1;
        [StringLength(300)] public string? Option1 { get; set; }
        [StringLength(300)] public string? Option2 { get; set; }
        [StringLength(300)] public string? Option3 { get; set; }
        [StringLength(300)] public string? Option4 { get; set; }
        [Range(1, 4)] public int CorrectOptionNumber { get; set; } = 1;
        [StringLength(200)] public string? CorrectAnswerText { get; set; }
        [Url] public string? AudioUrl { get; set; }
        [StringLength(200)] public string? ToneSequence { get; set; }
    }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var quiz = await GetOwnedQuizAsync(id);
        if (quiz is null) return NotFound();
        Quiz = quiz;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int id)
    {
        var quiz = await GetOwnedQuizAsync(id);
        if (quiz is null) return NotFound();
        Quiz = quiz;

        var options = BuildOptions(Input);
        if (Input.Type is QuestionType.MultipleChoice or QuestionType.TrueFalse)
        {
            if (Input.Type == QuestionType.MultipleChoice && options.Count < 2)
                ModelState.AddModelError(string.Empty, "Enter at least two answer options.");
            if (Input.Type == QuestionType.MultipleChoice &&
                (string.IsNullOrWhiteSpace(Input.Option1) || string.IsNullOrWhiteSpace(Input.Option2)))
                ModelState.AddModelError(string.Empty, "Provide the first two answer options.");
            if (Input.CorrectOptionNumber > options.Count)
                ModelState.AddModelError(nameof(Input.CorrectOptionNumber), "Choose one of the provided options.");
        }
        else if (string.IsNullOrWhiteSpace(Input.CorrectAnswerText))
        {
            ModelState.AddModelError(nameof(Input.CorrectAnswerText), "Enter the expected answer.");
        }

        if (Input.Type == QuestionType.AudioIdentification &&
            string.IsNullOrWhiteSpace(Input.AudioUrl) &&
            string.IsNullOrWhiteSpace(Input.ToneSequence))
        {
            ModelState.AddModelError(string.Empty, "Add an audio URL or comma-separated tone frequencies.");
        }

        if (!ModelState.IsValid) return Page();

        var question = new QuizQuestion
        {
            QuizId = quiz.Id,
            QuestionText = Input.QuestionText.Trim(),
            Type = Input.Type,
            Points = Input.Points,
            OrderIndex = quiz.Questions.Count + 1,
            CorrectAnswerIndex = Input.Type is QuestionType.MultipleChoice or QuestionType.TrueFalse
                ? Input.CorrectOptionNumber - 1
                : null,
            OptionsJson = Input.Type is QuestionType.MultipleChoice or QuestionType.TrueFalse
                ? JsonSerializer.Serialize(options)
                : null,
            CorrectAnswerText = Input.CorrectAnswerText?.Trim(),
            AudioUrl = Input.AudioUrl,
            ToneSequence = Input.ToneSequence?.Trim()
        };
        _db.QuizQuestions.Add(question);
        await _db.SaveChangesAsync();
        TempData["StatusMessage"] = "Question added.";
        return RedirectToPage("./Manage", new { id });
    }

    private async Task<NoryMusicLMS_VS.Models.Quiz?> GetOwnedQuizAsync(int id)
    {
        var userId = _users.GetUserId(User);
        return await _db.Quizzes
            .Include(q => q.Questions.OrderBy(question => question.OrderIndex))
            .Include(q => q.Lesson)
                .ThenInclude(l => l.Course)
            .FirstOrDefaultAsync(q => q.Id == id && q.Lesson.Course.InstructorId == userId);
    }

    private static List<string> BuildOptions(QuestionInput input)
    {
        if (input.Type == QuestionType.TrueFalse)
            return ["True", "False"];
        return new[] { input.Option1, input.Option2, input.Option3, input.Option4 }
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value!.Trim())
            .ToList();
    }
}
