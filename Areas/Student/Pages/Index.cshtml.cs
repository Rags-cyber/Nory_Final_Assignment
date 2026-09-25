using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using NoryMusicLMS_VS.Data;
using NoryMusicLMS_VS.Models;

namespace NoryMusicLMS_VS.Areas.Student.Pages
{
    [Authorize(Roles = "Student")]
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public IndexModel(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public IList<Enrollment> Enrollments { get; set; } = default!;
        public IList<StudentAward> Awards { get; set; } = new List<StudentAward>();
        public IList<QuizAttempt> RecentAttempts { get; set; } = new List<QuizAttempt>();
        public ApplicationUser CurrentUser { get; set; } = default!;

        public async Task OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user != null)
            {
                CurrentUser = user;
                Enrollments = await _context.Enrollments
                    .Include(e => e.Course)
                    .Where(e => e.StudentId == user.Id && e.Status != EnrollmentStatus.Dropped)
                    .OrderByDescending(e => e.EnrolledAt)
                    .ToListAsync();
                Awards = await _context.StudentAwards
                    .Where(a => a.StudentId == user.Id)
                    .OrderByDescending(a => a.EarnedAt)
                    .Take(6)
                    .ToListAsync();
                RecentAttempts = await _context.QuizAttempts
                    .Include(a => a.Quiz)
                    .Where(a => a.StudentId == user.Id && a.CompletedAt != null)
                    .OrderByDescending(a => a.CompletedAt)
                    .Take(5)
                    .ToListAsync();
            }
        }
    }
}
