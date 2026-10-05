using System.ComponentModel.DataAnnotations;

namespace NoryMusicLMS_VS.Models;

public class ChordSong
{
    public int Id { get; set; }

    [Required, StringLength(150)]
    public string Title { get; set; } = "";

    [Required, StringLength(150)]
    public string Artist { get; set; } = "";

    [Required, StringLength(40)]
    public string Instrument { get; set; } = "Guitar";

    [StringLength(20)]
    public string? Key { get; set; }

    public int? CapoFret { get; set; }
    public int? TempoBpm { get; set; }

    [StringLength(80)]
    public string? TempoNote { get; set; }

    
    [Required, StringLength(2000)]
    public string ChordMap { get; set; } = "";

    [StringLength(250)]
    public string? StrummingPattern { get; set; }

    [StringLength(1000)]
    public string? AttributionUrl { get; set; }

    
    [StringLength(200)]
    public string? ImageUrl { get; set; }

    public DateTime AddedAt { get; set; } = DateTime.UtcNow;
}
