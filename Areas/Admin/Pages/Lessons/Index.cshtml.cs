using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using NoryMusicLMS_VS.Data;
using NoryMusicLMS_VS.Models;

namespace NoryMusicLMS_VS.Areas.Admin.Pages.Lessons
{
    [Authorize(Roles = "Admin")]
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public IList<Lesson> Lessons { get; set; } = default!;

        public async Task OnGetAsync()
        {
            Lessons = await _context.Lessons
                .Include(l => l.Course)
                .OrderBy(l => l.Course.Title)
                .ThenBy(l => l.OrderIndex)
                .ToListAsync();
        }
    }
}