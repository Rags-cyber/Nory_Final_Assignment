using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using NoryMusicLMS_VS.Data;
using NoryMusicLMS_VS.Models;
using NoryMusicLMS_VS.Services;

namespace NoryMusicLMS_VS.Areas.Admin.Pages.Courses;
[Authorize(Roles="Admin")]
public class EditModel : PageModel
{
    private readonly ApplicationDbContext _db; private readonly ImageStorageService _images;
    public EditModel(ApplicationDbContext db, ImageStorageService images){_db=db;_images=images;}
    [BindProperty] public CourseInput Input {get;set;}=new(); public int CourseId{get;private set;} public string? CurrentImageUrl{get;private set;}
    public class CourseInput{[System.ComponentModel.DataAnnotations.Required, System.ComponentModel.DataAnnotations.StringLength(100)]public string Title{get;set;}="";[System.ComponentModel.DataAnnotations.StringLength(500)]public string Description{get;set;}="";[System.ComponentModel.DataAnnotations.Required]public string InstructorId{get;set;}="";public DateTime StartDate{get;set;}=DateTime.Today;public DateTime EndDate{get;set;}=DateTime.Today.AddMonths(3);public bool IsActive{get;set;}=true;public IFormFile? Image{get;set;}public bool RemoveImage{get;set;}}
    public async Task<IActionResult> OnGetAsync(int id){var c=await _db.Courses.FindAsync(id);if(c is null)return NotFound();CourseId=id;CurrentImageUrl=c.ImageUrl;Input=new(){Title=c.Title,Description=c.Description,InstructorId=c.InstructorId,StartDate=c.StartDate,EndDate=c.EndDate,IsActive=c.IsActive};await LoadInstructorsAsync();return Page();}
    public async Task<IActionResult> OnPostAsync(int id){var c=await _db.Courses.FindAsync(id);if(c is null)return NotFound();CourseId=id;CurrentImageUrl=c.ImageUrl;if(Input.EndDate<Input.StartDate)ModelState.AddModelError("Input.EndDate","End date must be on or after the start date.");if(!await IsInstructorAsync(Input.InstructorId))ModelState.AddModelError("Input.InstructorId","Select a valid instructor.");if(ImageStorageService.Validate(Input.Image) is { } e)ModelState.AddModelError("Input.Image",e);if(!ModelState.IsValid){await LoadInstructorsAsync();return Page();}c.Title=Input.Title.Trim();c.Description=Input.Description.Trim();c.InstructorId=Input.InstructorId;c.StartDate=Input.StartDate;c.EndDate=Input.EndDate;c.IsActive=Input.IsActive;c.ImageUrl=await _images.ApplyAsync(c.ImageUrl,Input.Image,Input.RemoveImage,null);await _db.SaveChangesAsync();TempData["StatusMessage"]="Course updated.";return RedirectToPage("./Index");}
    private async Task<bool> IsInstructorAsync(string id)=>await _db.UserRoles.AnyAsync(ur=>ur.UserId==id&&_db.Roles.Any(r=>r.Id==ur.RoleId&&r.Name=="Instructor"));
    private async Task LoadInstructorsAsync()=>ViewData["InstructorId"]=new SelectList(await _db.Users.Where(u=>_db.UserRoles.Any(ur=>ur.UserId==u.Id&&_db.Roles.Any(r=>r.Id==ur.RoleId&&r.Name=="Instructor"))).Select(u=>new{u.Id,FullName=u.FirstName+" "+u.LastName}).ToListAsync(),"Id","FullName",Input.InstructorId);
}
