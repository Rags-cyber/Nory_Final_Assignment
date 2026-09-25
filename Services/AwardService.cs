using Microsoft.EntityFrameworkCore;
using NoryMusicLMS_VS.Data;
using NoryMusicLMS_VS.Models;

namespace NoryMusicLMS_VS.Services;

public class AwardService
{
    private readonly ApplicationDbContext _db;

    public AwardService(ApplicationDbContext db) => _db = db;

    public async Task<bool> GrantOnceAsync(string studentId, string awardKey, string title, string description)
    {
        var exists = await _db.StudentAwards
            .AnyAsync(a => a.StudentId == studentId && a.AwardKey == awardKey);
        if (exists) return false;

        _db.StudentAwards.Add(new StudentAward
        {
            StudentId = studentId,
            AwardKey = awardKey,
            Title = title,
            Description = description,
            EarnedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();
        return true;
    }
}
