using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using NoryMusicLMS_VS.Data;
using NoryMusicLMS_VS.Models;

namespace NoryMusicLMS_VS.Areas.Instructor.Pages.ChordSongs;


[Authorize(Roles = "Instructor")]
public class IndexModel : PageModel
{
    private readonly ApplicationDbContext _db;
    public IndexModel(ApplicationDbContext db) => _db = db;

    public IList<ChordSong> Songs { get; private set; } = new List<ChordSong>();

    public async Task OnGetAsync()
    {
        Songs = await _db.ChordSongs.AsNoTracking().OrderBy(s => s.Title).ToListAsync();
    }
}
