using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using NoryMusicLMS_VS.Models;

namespace NoryMusicLMS_VS.Areas.Admin.Pages.Users;

[Authorize(Roles = "Admin")]
public class CreateModel : PageModel
{
    private readonly UserManager<ApplicationUser> _users;
    private readonly RoleManager<IdentityRole> _roles;

    public CreateModel(UserManager<ApplicationUser> users, RoleManager<IdentityRole> roles)
    {
        _users = users;
        _roles = roles;
    }

    [BindProperty]
    public InputData Input { get; set; } = new();

    public class InputData
    {
        [Required, StringLength(50)] public string FirstName { get; set; } = "";
        [Required, StringLength(50)] public string LastName { get; set; } = "";
        [Required, EmailAddress] public string Email { get; set; } = "";
        [Required, StringLength(100, MinimumLength = 6), DataType(DataType.Password)]
        public string Password { get; set; } = "";
        [Required] public string Role { get; set; } = "Student";
    }

    public async Task<IActionResult> OnGetAsync()
    {
        if (!await RolesExistAsync()) return RedirectToPage("./Index");
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!await RolesExistAsync()) return RedirectToPage("./Index");
        if (!new[] { "Student", "Instructor", "Admin" }.Contains(Input.Role))
            ModelState.AddModelError("Input.Role", "Select a valid role.");
        if (!ModelState.IsValid) return Page();

        var email = Input.Email.Trim();
        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            FirstName = Input.FirstName.Trim(),
            LastName = Input.LastName.Trim(),
            EmailConfirmed = true
        };

        var created = await _users.CreateAsync(user, Input.Password);
        if (!created.Succeeded)
        {
            AddErrors(created);
            return Page();
        }

        var addedRole = await _users.AddToRoleAsync(user, Input.Role);
        if (!addedRole.Succeeded)
        {
            await _users.DeleteAsync(user);
            AddErrors(addedRole);
            return Page();
        }

        TempData["StatusMessage"] = $"Created {Input.Role.ToLowerInvariant()} account for {email}.";
        return RedirectToPage("./Index");
    }

    private async Task<bool> RolesExistAsync()
    {
        foreach (var role in new[] { "Student", "Instructor", "Admin" })
        {
            if (await _roles.RoleExistsAsync(role)) continue;
            ModelState.AddModelError(string.Empty, $"The {role} role is missing. Restart the app to seed roles.");
            return false;
        }
        return true;
    }

    private void AddErrors(IdentityResult result)
    {
        foreach (var error in result.Errors)
            ModelState.AddModelError(string.Empty, error.Description);
    }
}
