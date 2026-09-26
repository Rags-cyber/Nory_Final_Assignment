using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using NoryMusicLMS_VS.Data;
using NoryMusicLMS_VS.Models;

namespace NoryMusicLMS_VS.Areas.Student.Pages.ChordSongs;

[Authorize(Roles = "Student")]
public class IndexModel : PageModel
{
    private readonly ApplicationDbContext _db;
    public IndexModel(ApplicationDbContext db) => _db = db;

    [BindProperty(SupportsGet = true)]
    public string? Search { get; set; }
    public IList<ChordSong> Songs { get; private set; } = new List<ChordSong>();

    public async Task OnGetAsync()
    {
        var query = _db.ChordSongs.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(Search))
        {
            var term = Search.Trim();
            query = query.Where(s => s.Title.Contains(term) || s.Artist.Contains(term) || s.Instrument.Contains(term));
        }
        Songs = await query.OrderBy(s => s.Title).ToListAsync();
    }
}
