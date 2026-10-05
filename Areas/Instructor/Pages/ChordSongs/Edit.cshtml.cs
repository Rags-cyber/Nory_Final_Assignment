using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using NoryMusicLMS_VS.Data;
using NoryMusicLMS_VS.Models;
using NoryMusicLMS_VS.Services;

namespace NoryMusicLMS_VS.Areas.Instructor.Pages.ChordSongs;

[Authorize(Roles = "Instructor")]
public class EditModel : PageModel
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _users;
    private readonly ImageStorageService _images;

    public EditModel(ApplicationDbContext db, UserManager<ApplicationUser> users, ImageStorageService images)
    {
        _db = db;
        _users = users;
        _images = images;
    }

    [BindProperty] public SongInput Input { get; set; } = new();
    public int SongId { get; private set; }
    public string? CurrentImageUrl { get; private set; }

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
        public bool RemoveImage { get; set; }
    }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var song = await _db.ChordSongs.FindAsync(id);
        if (song is null) return NotFound();
        SongId = song.Id;
        CurrentImageUrl = song.ImageUrl;
        Input = new SongInput
        {
            Title = song.Title,
            Artist = song.Artist,
            Instrument = song.Instrument,
            Key = song.Key,
            CapoFret = song.CapoFret,
            TempoBpm = song.TempoBpm,
            TempoNote = song.TempoNote,
            ChordMap = song.ChordMap,
            StrummingPattern = song.StrummingPattern,
            AttributionUrl = song.AttributionUrl
        };
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int id)
    {
        SongId = id;
        var song = await _db.ChordSongs.FindAsync(id);
        if (song is null) return NotFound();
        CurrentImageUrl = song.ImageUrl;

        if (ImageStorageService.Validate(Input.Image) is { } imageError)
            ModelState.AddModelError("Input.Image", imageError);
        if (!ModelState.IsValid) return Page();

        song.Title = Input.Title.Trim();
        song.Artist = Input.Artist.Trim();
        song.Instrument = Input.Instrument.Trim();
        song.Key = Input.Key?.Trim();
        song.CapoFret = Input.CapoFret;
        song.TempoBpm = Input.TempoBpm;
        song.TempoNote = Input.TempoNote?.Trim();
        song.ChordMap = Input.ChordMap.Trim();
        song.StrummingPattern = Input.StrummingPattern?.Trim();
        song.AttributionUrl = Input.AttributionUrl;
        song.ImageUrl = await _images.ApplyAsync(song.ImageUrl, Input.Image, Input.RemoveImage, _users.GetUserId(User));
        await _db.SaveChangesAsync();
        TempData["StatusMessage"] = "Song updated.";
        return RedirectToPage("./Index");
    }
}
