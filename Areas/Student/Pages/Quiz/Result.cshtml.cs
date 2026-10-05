using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using NoryMusicLMS_VS.Data;
using NoryMusicLMS_VS.Models;
using Quiz = NoryMusicLMS_VS.Models.Quiz;

namespace NoryMusicLMS_VS.Areas.Student.Pages.Quiz
{
    [Authorize(Roles = "Student")]
    public class ResultModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ResultModel(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public QuizAttempt Attempt { get; set; } = default!;
        public NoryMusicLMS_VS.Models.Quiz Quiz { get; set; } = default!;
        public IList<QuizAnswer> Answers { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int? attemptId)
        {
            if (attemptId == null)
            {
                return NotFound();
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            
            Attempt = await _context.QuizAttempts
                .Include(a => a.Quiz)
                    .ThenInclude(q => q.Lesson)
                        .ThenInclude(l => l.Course)
                .FirstOrDefaultAsync(a => a.Id == attemptId && a.StudentId == user.Id);

            if (Attempt == null)
            {
                return NotFound();
            }

            Quiz = Attempt.Quiz;
            Answers = await _context.QuizAnswers
                .Where(a => a.QuizAttemptId == attemptId)
                .Include(a => a.Question)
                .OrderBy(a => a.AnsweredAt)
                .ToListAsync();

            return Page();
        }
    }
}