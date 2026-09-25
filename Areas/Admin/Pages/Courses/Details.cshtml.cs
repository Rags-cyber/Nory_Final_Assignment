using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using NoryMusicLMS_VS.Data;
using NoryMusicLMS_VS.Models;

namespace NoryMusicLMS_VS.Areas.Admin.Pages.Courses
{
    [Authorize(Roles = "Admin")]
    public class DetailsModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public DetailsModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public Course Course { get; set; } = default!;
        public IList<Lesson> Lessons { get; set; } = default!;

        public async Task OnGetAsync(int id)
        {
            Course = await _context.Courses
                .Include(c => c.Instructor)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (Course == null)
            {
                NotFound();
            }
            else
            {
                Lessons = await _context.Lessons
                    .Where(l => l.CourseId == id)
                    .OrderBy(l => l.OrderIndex)
                    .ToListAsync();
            }
        }
    }
}