using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using NoryMusicLMS_VS.Data;
using NoryMusicLMS_VS.Models;
using NoryMusicLMS_VS.Services;

namespace NoryMusicLMS_VS.Areas.Instructor.Pages.ChordSongs;

[Authorize(Roles = "Instructor")]
public class CreateModel : PageModel
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _users;
    private readonly ImageStorageService _images;

    public CreateModel(ApplicationDbContext db, UserManager<ApplicationUser> users, ImageStorageService images)
    {
        _db = db;
        _users = users;
        _images = images;
    }

    [BindProperty] public SongInput Input { get; set; } = new();

    public class SongInput
    {
        [Required, StringLength(150)] public string Title { get; set; } = "";
        [Required, StringLength(150)] public string Artist { get; set; } = "";
        [Required, StringLength(40)] public string Instrument { get; set; } = "Guitar";
        [StringLength(20)] public string? Key { get; set; }
        [Range(0, 12)] public int? CapoFret { get; set; }
        [Range(20, 300)] public int? TempoBpm { get; set; }
        [StringLength(80)] public string? TempoNote { get; set; }
        [Required, StringLength(2000)] public string ChordMap { get; set; } = "";
        [StringLength(250)] public string? StrummingPattern { get; set; }
        [Url, StringLength(1000)] public string? AttributionUrl { get; set; }
        public IFormFile? Image { get; set; }
    }

    public IActionResult OnGet() => Page();

    public async Task<IActionResult> OnPostAsync()
    {
        if (ImageStorageService.Validate(Input.Image) is { } imageError)
            ModelState.AddModelError("Input.Image", imageError);
        if (!ModelState.IsValid) return Page();

        var imageUrl = await _images.ApplyAsync(null, Input.Image, false, _users.GetUserId(User));

        _db.ChordSongs.Add(new ChordSong
        {
            Title = Input.Title.Trim(),
            Artist = Input.Artist.Trim(),
            Instrument = Input.Instrument.Trim(),
            Key = Input.Key?.Trim(),
            CapoFret = Input.CapoFret,
            TempoBpm = Input.TempoBpm,
            TempoNote = Input.TempoNote?.Trim(),
            ChordMap = Input.ChordMap.Trim(),
            StrummingPattern = Input.StrummingPattern?.Trim(),
            AttributionUrl = Input.AttributionUrl,
            ImageUrl = imageUrl,
            AddedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();
        TempData["StatusMessage"] = "Song added.";
        return RedirectToPage("./Index");
    }
}
