using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using NoryMusicLMS_VS.Data;
using NoryMusicLMS_VS.Models;

namespace NoryMusicLMS_VS.Areas.Admin.Pages.QuizQuestions
{
    [Authorize(Roles = "Admin")]
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public IList<QuizQuestion> QuizQuestions { get; set; } = default!;

        public async Task OnGetAsync()
        {
            QuizQuestions = await _context.QuizQuestions
                .Include(q => q.Quiz)
                    .ThenInclude(q => q.Lesson)
                .OrderBy(q => q.QuizId)
                .ThenBy(q => q.OrderIndex)
                .ToListAsync();
        }
    }
}