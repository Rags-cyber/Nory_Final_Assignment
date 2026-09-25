using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using NoryMusicLMS_VS.Data;
using NoryMusicLMS_VS.Models;

namespace NoryMusicLMS_VS.Areas.Admin.Pages.Resources
{
    [Authorize(Roles = "Admin")]
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public IList<Resource> Resources { get; set; } = default!;

        public async Task OnGetAsync()
        {
            Resources = await _context.Resources
                .Include(r => r.Lesson)
                .ThenInclude(l => l.Course)
                .OrderByDescending(r => r.UploadedAt)
                .ToListAsync();
        }
    }
}
