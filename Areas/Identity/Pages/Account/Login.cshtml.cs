using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using NoryMusicLMS_VS.Models;

namespace NoryMusicLMS_VS.Areas.Identity.Pages.Account;

public class LoginModel : PageModel
{
    private readonly SignInManager<ApplicationUser> _signIn;
    private readonly UserManager<ApplicationUser> _users;

    public LoginModel(SignInManager<ApplicationUser> signIn, UserManager<ApplicationUser> users)
    {
        _signIn = signIn;
        _users = users;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public string? ReturnUrl { get; set; }

    public class InputModel
    {
        [Required, EmailAddress]
        public string Email { get; set; } = "";

        [Required, DataType(DataType.Password)]
        public string Password { get; set; } = "";

        [Display(Name = "Remember me")]
        public bool RememberMe { get; set; }
    }

    public void OnGet() => ReturnUrl ??= Url.Content("~/");

    public async Task<IActionResult> OnPostAsync()
    {
        ReturnUrl ??= Url.Content("~/");
        if (!ModelState.IsValid)
            return Page();

        var user = await _users.FindByEmailAsync(Input.Email.Trim());
        if (user is null)
        {
            ModelState.AddModelError(string.Empty, "Invalid email or password.");
            return Page();
        }

        var result = await _signIn.PasswordSignInAsync(
            user.UserName!, Input.Password, Input.RememberMe, lockoutOnFailure: true);

        if (result.IsLockedOut)
        {
            ModelState.AddModelError(string.Empty, "This account is temporarily locked. Please try again later.");
            return Page();
        }

        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, "Invalid email or password.");
            return Page();
        }

        if (Url.IsLocalUrl(ReturnUrl) && ReturnUrl != "/" && !ReturnUrl.StartsWith("/Identity/", StringComparison.OrdinalIgnoreCase))
            return LocalRedirect(ReturnUrl);

        if (await _users.IsInRoleAsync(user, "Admin"))
            return LocalRedirect("/Admin");
        if (await _users.IsInRoleAsync(user, "Instructor"))
            return LocalRedirect("/Instructor");
        if (await _users.IsInRoleAsync(user, "Student"))
            return LocalRedirect("/Student");

        await _signIn.SignOutAsync();
        ModelState.AddModelError(string.Empty, "This account has no application role. Contact an administrator.");
        return Page();
    }
}
