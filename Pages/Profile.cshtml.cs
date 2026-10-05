using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using NoryMusicLMS_VS.Models;

namespace NoryMusicLMS_VS.Pages;

[Authorize]
public class ProfileModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;

    public ProfileModel(UserManager<ApplicationUser> userManager) => _userManager = userManager;

    [BindProperty]
    public ProfileInput Input { get; set; } = new();

    public bool IsStudent { get; private set; }
    public bool IsInstructor { get; private set; }
    public bool IsAdmin { get; private set; }
    public string Email { get; private set; } = "";

    public class ProfileInput
    {
        [Required, StringLength(50)]
        [Display(Name = "First name")]
        public string FirstName { get; set; } = "";

        [Required, StringLength(50)]
        [Display(Name = "Last name")]
        public string LastName { get; set; } = "";

        [Phone, StringLength(30)]
        [Display(Name = "Phone number")]
        public string? PhoneNumber { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Date of birth")]
        public DateTime? DateOfBirth { get; set; }

        [StringLength(1000)]
        public string? Bio { get; set; }

        [StringLength(100)]
        public string? Instrument { get; set; }

        [StringLength(40)]
        [Display(Name = "Current level")]
        public string? SkillLevel { get; set; }

        [StringLength(1000)]
        [Display(Name = "Learning goals")]
        public string? LearningGoals { get; set; }

        [StringLength(150)]
        [Display(Name = "Teaching specialty")]
        public string? TeachingSpecialty { get; set; }

        [Range(0, 80)]
        [Display(Name = "Years teaching")]
        public int? YearsTeaching { get; set; }

        [StringLength(100)]
        [Display(Name = "Department")]
        public string? AdminDepartment { get; set; }

        [StringLength(100)]
        [Display(Name = "Job title")]
        public string? AdminJobTitle { get; set; }
    }

    public async Task<IActionResult> OnGetAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Challenge();

        await LoadRoleSectionsAsync(user);
        Email = user.Email ?? "";
        Input = new ProfileInput
        {
            FirstName = user.FirstName,
            LastName = user.LastName,
            PhoneNumber = user.PhoneNumber,
            DateOfBirth = user.DateOfBirth == default ? null : user.DateOfBirth,
            Bio = user.Bio,
            Instrument = user.Instrument,
            SkillLevel = user.SkillLevel,
            LearningGoals = user.LearningGoals,
            TeachingSpecialty = user.TeachingSpecialty,
            YearsTeaching = user.YearsTeaching,
            AdminDepartment = user.AdminDepartment,
            AdminJobTitle = user.AdminJobTitle
        };

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Challenge();

        await LoadRoleSectionsAsync(user);
        Email = user.Email ?? "";
        if (!ModelState.IsValid) return Page();

        user.FirstName = Input.FirstName.Trim();
        user.LastName = Input.LastName.Trim();
        user.PhoneNumber = string.IsNullOrWhiteSpace(Input.PhoneNumber) ? null : Input.PhoneNumber.Trim();
        if (Input.DateOfBirth.HasValue)
            user.DateOfBirth = Input.DateOfBirth.Value.Date;
        user.Bio = Clean(Input.Bio);

        
        
        if (IsStudent)
        {
            user.Instrument = Clean(Input.Instrument);
            user.SkillLevel = Clean(Input.SkillLevel);
            user.LearningGoals = Clean(Input.LearningGoals);
        }
        if (IsInstructor)
        {
            user.TeachingSpecialty = Clean(Input.TeachingSpecialty);
            user.YearsTeaching = Input.YearsTeaching;
        }
        if (IsAdmin)
        {
            user.AdminDepartment = Clean(Input.AdminDepartment);
            user.AdminJobTitle = Clean(Input.AdminJobTitle);
        }

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);
            return Page();
        }

        TempData["ProfileSaved"] = "Your profile has been saved.";
        return RedirectToPage();
    }

    private async Task LoadRoleSectionsAsync(ApplicationUser user)
    {
        var roles = await _userManager.GetRolesAsync(user);
        IsStudent = roles.Contains("Student");
        IsInstructor = roles.Contains("Instructor");
        IsAdmin = roles.Contains("Admin");
    }

    private static string? Clean(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
