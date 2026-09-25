using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using NoryMusicLMS_VS.Data;
using NoryMusicLMS_VS.Models;

namespace NoryMusicLMS_VS.Areas.Admin.Pages.Users;

[Authorize(Roles = "Admin")]
public class DeleteModel : PageModel
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _users;

    public DeleteModel(ApplicationDbContext db, UserManager<ApplicationUser> users)
    {
        _db = db;
        _users = users;
    }

    public ApplicationUser Account { get; private set; } = default!;
    public string? ErrorMessage { get; private set; }

    public async Task<IActionResult> OnGetAsync(string id)
    {
        var result = await LoadAsync(id);
        return result ?? Page();
    }

    public async Task<IActionResult> OnPostAsync(string id)
    {
        var result = await LoadAsync(id);
        if (result is not null) return result;

        if (Account.Id == _users.GetUserId(User))
        {
            ErrorMessage = "You cannot delete your own account.";
            return Page();
        }

        var roles = await _users.GetRolesAsync(Account);
        if (roles.Contains("Admin") && (await _users.GetUsersInRoleAsync("Admin")).Count <= 1)
        {
            ErrorMessage = "The last administrator account cannot be deleted.";
            return Page();
        }

        var hasRecords =
            await _db.Courses.AnyAsync(c => c.InstructorId == Account.Id) ||
            await _db.Enrollments.AnyAsync(e => e.StudentId == Account.Id) ||
            await _db.QuizAttempts.AnyAsync(a => a.StudentId == Account.Id) ||
            await _db.LessonCompletions.AnyAsync(c => c.StudentId == Account.Id) ||
            await _db.StudentAwards.AnyAsync(a => a.StudentId == Account.Id);
        if (hasRecords)
        {
            ErrorMessage = "This account has course or learning records. Reassign or preserve those records before deleting it.";
            return Page();
        }

        var deleted = await _users.DeleteAsync(Account);
        if (!deleted.Succeeded)
        {
            ErrorMessage = string.Join(" ", deleted.Errors.Select(e => e.Description));
            return Page();
        }

        TempData["StatusMessage"] = "User account deleted.";
        return RedirectToPage("./Index");
    }

    private async Task<IActionResult?> LoadAsync(string id)
    {
        Account = await _users.FindByIdAsync(id) ?? default!;
        return Account is null ? NotFound() : null;
    }
}
