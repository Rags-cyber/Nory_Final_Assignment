using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using NoryMusicLMS_VS.Data;
using NoryMusicLMS_VS.Models;
using NoryMusicLMS_VS.Services;
using System.Text.Json;
using Quiz = NoryMusicLMS_VS.Models.Quiz;

namespace NoryMusicLMS_VS.Areas.Student.Pages.Quiz
{
    [Authorize(Roles = "Student")]
    public class TakeModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly AwardService _awardService;

        public TakeModel(ApplicationDbContext context, UserManager<ApplicationUser> userManager, AwardService awardService)
        {
            _context = context;
            _userManager = userManager;
            _awardService = awardService;
        }

        public NoryMusicLMS_VS.Models.Quiz Quiz { get; set; } = default!;
        public IList<QuizQuestion> Questions { get; set; } = default!;
        public QuizAttempt Attempt { get; set; } = default!;

        [BindProperty]
        public List<QuizAnswerViewModel> Answers { get; set; } = new List<QuizAnswerViewModel>();

        public class QuizAnswerViewModel
        {
            public int QuestionId { get; set; }
            public string SelectedAnswer { get; set; } = string.Empty;
            public int? SelectedAnswerIndex { get; set; }
        }

        public async Task<IActionResult> OnGetAsync(int? quizId)
        {
            if (quizId == null)
            {
                return NotFound();
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            // Get the quiz with its questions
            Quiz = await _context.Quizzes
                .Include(q => q.Lesson)
                    .ThenInclude(l => l.Course)
                .FirstOrDefaultAsync(q => q.Id == quizId);

            if (Quiz == null)
            {
                return NotFound();
            }

            // Check if user is enrolled in this course
            var enrollment = await _context.Enrollments
                .FirstOrDefaultAsync(e => e.StudentId == user.Id && e.CourseId == Quiz.Lesson.CourseId);

            if (enrollment == null || enrollment.Status != EnrollmentStatus.Active)
            {
                TempData["ErrorMessage"] = "You need to be enrolled in this course to take quizzes.";
                return RedirectToPage("/Student/BrowseCourses");
            }

            // Get questions for this quiz
            Questions = await _context.QuizQuestions
                .Where(q => q.QuizId == quizId)
                .OrderBy(q => q.OrderIndex)
                .ToListAsync();
            if (Questions.Count == 0)
                return BadRequest("This quiz has no questions yet.");

            // Ignore forged question IDs and accept only answers for this quiz's
            // actual questions; radio option indices are zero-based.
            Answers = (Answers ?? new List<QuizAnswerViewModel>())
                .Where(a => Questions.Any(q => q.Id == a.QuestionId))
                .GroupBy(a => a.QuestionId)
                .Select(g => g.Last())
                .ToList();

            // Initialize answer view models
            foreach (var question in Questions)
            {
                Answers.Add(new QuizAnswerViewModel
                {
                    QuestionId = question.Id
                });
            }

            // Check if there's an existing attempt for this user and quiz
            Attempt = await _context.QuizAttempts
                .Include(a => a.Quiz)
                .FirstOrDefaultAsync(a => a.StudentId == user.Id && a.QuizId == quizId && a.CompletedAt == null);

            if (Attempt != null)
            {
                // Load existing answers
                var existingAnswers = await _context.QuizAnswers
                    .Where(a => a.QuizAttemptId == Attempt.Id)
                    .ToListAsync();

                foreach (var answer in existingAnswers)
                {
                    var viewModel = Answers.FirstOrDefault(a => a.QuestionId == answer.QuizQuestionId);
                    if (viewModel != null)
                    {
                        viewModel.SelectedAnswer = answer.SelectedAnswer;
                        viewModel.SelectedAnswerIndex = answer.SelectedAnswerIndex;
                    }
                }
            }

            return Page();
        }

        public async Task<IActionResult> OnPostSubmitAsync(int? quizId)
        {
            if (quizId == null)
            {
                return NotFound();
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            // Get the quiz
            Quiz = await _context.Quizzes
                .Include(q => q.Lesson)
                    .ThenInclude(l => l.Course)
                .FirstOrDefaultAsync(q => q.Id == quizId);

            if (Quiz == null)
            {
                return NotFound();
            }

            // Check enrollment
            var enrollment = await _context.Enrollments
                .FirstOrDefaultAsync(e => e.StudentId == user.Id && e.CourseId == Quiz.Lesson.CourseId);

            if (enrollment == null || enrollment.Status != EnrollmentStatus.Active)
            {
                TempData["ErrorMessage"] = "You need to be enrolled in this course to take quizzes.";
                return RedirectToPage("/Student/BrowseCourses");
            }

            // Get questions
            Questions = await _context.QuizQuestions
                .Where(q => q.QuizId == quizId)
                .OrderBy(q => q.OrderIndex)
                .ToListAsync();

            // Check if there's an existing attempt
            Attempt = await _context.QuizAttempts
                .FirstOrDefaultAsync(a => a.StudentId == user.Id && a.QuizId == quizId && a.CompletedAt == null);

            if (Attempt == null)
            {
                // Create new attempt
                Attempt = new QuizAttempt
                {
                    StudentId = user.Id,
                    QuizId = quizId.Value,
                    StartedAt = DateTime.UtcNow
                };

                _context.QuizAttempts.Add(Attempt);
                await _context.SaveChangesAsync();
            }

            // Process answers
            int totalPoints = 0;
            int earnedPoints = 0;

            foreach (var question in Questions)
            {
                var answerViewModel = Answers.FirstOrDefault(a => a.QuestionId == question.Id);
                if (answerViewModel == null) continue;

                bool isCorrect = false;
                int pointsEarned = 0;
                string feedback = string.Empty;

                switch (question.Type)
                {
                    case QuestionType.MultipleChoice:
                        if (answerViewModel.SelectedAnswerIndex.HasValue &&
                            TryGetOptionCount(question.OptionsJson, out var optionCount) &&
                            answerViewModel.SelectedAnswerIndex.Value >= 0 &&
                            answerViewModel.SelectedAnswerIndex.Value < optionCount &&
                            answerViewModel.SelectedAnswerIndex == question.CorrectAnswerIndex)
                        {
                            isCorrect = true;
                            pointsEarned = question.Points;
                            feedback = "Correct!";
                        }
                        else
                        {
                            feedback = "Incorrect. The correct answer is option " + question.CorrectAnswerIndex;
                        }
                        break;

                    case QuestionType.TrueFalse:
                        if (answerViewModel.SelectedAnswerIndex.HasValue &&
                            answerViewModel.SelectedAnswerIndex.Value is 0 or 1 &&
                            answerViewModel.SelectedAnswerIndex == question.CorrectAnswerIndex)
                        {
                            isCorrect = true;
                            pointsEarned = question.Points;
                            feedback = "Correct!";
                        }
                        else
                        {
                            feedback = "Incorrect.";
                        }
                        break;

                    case QuestionType.FillInBlank:
                        if (!string.IsNullOrEmpty(answerViewModel.SelectedAnswer) &&
                            !string.IsNullOrEmpty(question.CorrectAnswerText) &&
                            answerViewModel.SelectedAnswer.Trim().ToLower() == question.CorrectAnswerText.Trim().ToLower())
                        {
                            isCorrect = true;
                            pointsEarned = question.Points;
                            feedback = "Correct!";
                        }
                        else
                        {
                            feedback = $"Incorrect. The correct answer is: {question.CorrectAnswerText}";
                        }
                        break;

                    case QuestionType.AudioIdentification:
                        if (!string.IsNullOrEmpty(answerViewModel.SelectedAnswer) &&
                            !string.IsNullOrEmpty(question.CorrectAnswerText) &&
                            answerViewModel.SelectedAnswer.Trim().ToLower() == question.CorrectAnswerText.Trim().ToLower())
                        {
                            isCorrect = true;
                            pointsEarned = question.Points;
                            feedback = "Correct! You identified the note correctly.";
                        }
                        else
                        {
                            feedback = $"Incorrect. The correct note is: {question.CorrectAnswerText}";
                        }
                        break;
                }

                totalPoints += question.Points;
                earnedPoints += pointsEarned;

                // Save or update answer
                var existingAnswer = await _context.QuizAnswers
                    .FirstOrDefaultAsync(a => a.QuizAttemptId == Attempt.Id && a.QuizQuestionId == question.Id);

                if (existingAnswer != null)
                {
                    existingAnswer.SelectedAnswer = answerViewModel.SelectedAnswer;
                    existingAnswer.SelectedAnswerIndex = answerViewModel.SelectedAnswerIndex;
                    existingAnswer.IsCorrect = isCorrect;
                    existingAnswer.PointsEarned = pointsEarned;
                    existingAnswer.Feedback = feedback;
                    existingAnswer.AnsweredAt = DateTime.UtcNow;
                }
                else
                {
                    var newAnswer = new QuizAnswer
                    {
                        QuizAttemptId = Attempt.Id,
                        QuizQuestionId = question.Id,
                        SelectedAnswer = answerViewModel.SelectedAnswer,
                        SelectedAnswerIndex = answerViewModel.SelectedAnswerIndex,
                        IsCorrect = isCorrect,
                        PointsEarned = pointsEarned,
                        Feedback = feedback,
                        AnsweredAt = DateTime.UtcNow
                    };

                    _context.QuizAnswers.Add(newAnswer);
                }
            }

            // Update attempt
            Attempt.CompletedAt = DateTime.UtcNow;
            Attempt.ScorePercentage = totalPoints > 0 ? (int)((double)earnedPoints / totalPoints * 100) : 0;
            Attempt.IsPassed = Attempt.ScorePercentage >= 60; // Passing grade is 60%

            await _context.SaveChangesAsync();

            if (Attempt.IsPassed)
            {
                var awardTitle = Quiz.Type == QuizType.EarTraining
                    ? "Ear Training: " + Quiz.Title
                    : "Quiz Passed: " + Quiz.Title;
                await _awardService.GrantOnceAsync(
                    user.Id,
                    $"quiz-pass-{Quiz.Id}",
                    awardTitle,
                    $"Passed the {Quiz.Type} quiz in {Quiz.Lesson.Course.Title}.");
            }

            // Update enrollment progress based on quiz completion
            await UpdateEnrollmentProgress(user.Id, Quiz.Lesson.CourseId);

            return RedirectToPage("./Result", new { attemptId = Attempt.Id });
        }

        private static bool TryGetOptionCount(string? optionsJson, out int count)
        {
            count = 0;
            try
            {
                count = JsonSerializer.Deserialize<List<string>>(optionsJson ?? "[]")?.Count ?? 0;
                return count > 0;
            }
            catch (JsonException)
            {
                return false;
            }
        }

        private async Task UpdateEnrollmentProgress(string userId, int courseId)
        {
            var enrollment = await _context.Enrollments
                .FirstOrDefaultAsync(e => e.StudentId == userId && e.CourseId == courseId);

            if (enrollment != null)
            {
                // Calculate progress based on completed lessons and quizzes
                // For simplicity, we'll base it on lesson completion (which is updated when lessons are marked complete)
                // In a real app, you might also factor in quiz scores
                await _context.SaveChangesAsync();
            }
        }
    }
}
