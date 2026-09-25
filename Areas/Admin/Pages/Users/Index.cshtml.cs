using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using NoryMusicLMS_VS.Data;
using NoryMusicLMS_VS.Models;

namespace NoryMusicLMS_VS.Areas.Admin.Pages.Users;

[Authorize(Roles = "Admin")]
public class IndexModel : PageModel
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _users;

    public IndexModel(ApplicationDbContext db, UserManager<ApplicationUser> users)
    {
        _db = db;
        _users = users;
    }

    public IList<UserRow> Users { get; private set; } = new List<UserRow>();
    public string? Search { get; private set; }

    public async Task OnGetAsync(string? search)
    {
        Search = search?.Trim();
        var query = _db.Users.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(Search))
        {
            var term = Search;
            query = query.Where(u => u.Email!.Contains(term) ||
                u.FirstName.Contains(term) || u.LastName.Contains(term));
        }

        var users = await query.OrderBy(u => u.LastName).ThenBy(u => u.FirstName).ToListAsync();
        foreach (var user in users)
            Users.Add(new UserRow(user, string.Join(", ", await _users.GetRolesAsync(user))));
    }

    public record UserRow(ApplicationUser User, string Roles);
}
