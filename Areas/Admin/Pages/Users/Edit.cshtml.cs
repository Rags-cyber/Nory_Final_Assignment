using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using NoryMusicLMS_VS.Models;

namespace NoryMusicLMS_VS.Areas.Admin.Pages.Users;

[Authorize(Roles = "Admin")]
public class EditModel : PageModel
{
    private static readonly string[] AppRoles = ["Student", "Instructor", "Admin"];
    private readonly UserManager<ApplicationUser> _users;

    public EditModel(UserManager<ApplicationUser> users) => _users = users;

    [BindProperty] public InputData Input { get; set; } = new();
    public string UserId { get; private set; } = "";

    public class InputData
    {
        [Required, StringLength(50)] public string FirstName { get; set; } = "";
        [Required, StringLength(50)] public string LastName { get; set; } = "";
        [Required, EmailAddress] public string Email { get; set; } = "";
        [Required] public string Role { get; set; } = "Student";
        public bool EmailConfirmed { get; set; }
    }

    public async Task<IActionResult> OnGetAsync(string id)
    {
        var user = await _users.FindByIdAsync(id);
        if (user is null) return NotFound();
        UserId = user.Id;
        Input = new InputData
        {
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email ?? "",
            EmailConfirmed = user.EmailConfirmed,
            Role = (await _users.GetRolesAsync(user)).FirstOrDefault(AppRoles.Contains) ?? "Student"
        };
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(string id)
    {
        UserId = id;
        if (!AppRoles.Contains(Input.Role))
            ModelState.AddModelError("Input.Role", "Select a valid role.");
        if (!ModelState.IsValid) return Page();

        var user = await _users.FindByIdAsync(id);
        if (user is null) return NotFound();
        var oldRoles = await _users.GetRolesAsync(user);

        if (User.IsInRole("Admin") && user.Id == _users.GetUserId(User) &&
            !string.Equals(Input.Role, "Admin", StringComparison.Ordinal))
        {
            ModelState.AddModelError(string.Empty, "You cannot remove your own administrator role.");
            return Page();
        }

        if (oldRoles.Contains("Admin") && Input.Role != "Admin" &&
            (await _users.GetUsersInRoleAsync("Admin")).Count <= 1)
        {
            ModelState.AddModelError(string.Empty, "The last administrator cannot be demoted.");
            return Page();
        }

        user.FirstName = Input.FirstName.Trim();
        user.LastName = Input.LastName.Trim();
        user.Email = Input.Email.Trim();
        user.UserName = Input.Email.Trim();
        user.EmailConfirmed = Input.EmailConfirmed;

        var updated = await _users.UpdateAsync(user);
        if (!updated.Succeeded)
        {
            AddErrors(updated);
            return Page();
        }

        if (oldRoles.Any())
        {
            var removed = await _users.RemoveFromRolesAsync(user, oldRoles);
            if (!removed.Succeeded)
            {
                AddErrors(removed);
                return Page();
            }
        }
        var added = await _users.AddToRoleAsync(user, Input.Role);
        if (!added.Succeeded)
        {
            AddErrors(added);
            return Page();
        }

        TempData["StatusMessage"] = "User account updated.";
        return RedirectToPage("./Index");
    }

    private void AddErrors(IdentityResult result)
    {
        foreach (var error in result.Errors)
            ModelState.AddModelError(string.Empty, error.Description);
    }
}
