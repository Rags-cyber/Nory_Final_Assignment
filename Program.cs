using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using NoryMusicLMS_VS.Data;
using NoryMusicLMS_VS.Models;
using NoryMusicLMS_VS.Services;

    var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages(options =>
{
    options.Conventions.AuthorizeAreaFolder("Admin", "/", "Admin");
    options.Conventions.AuthorizeAreaFolder("Instructor", "/", "Instructor");
    options.Conventions.AuthorizeAreaFolder("Student", "/", "Student");
});

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddScoped<AwardService>();
builder.Services.AddScoped<ImageStorageService>();
builder.Services.AddScoped<AudioStorageService>();

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 6;
    options.Password.RequiredUniqueChars = 1;

    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;

    options.User.AllowedUserNameCharacters =
        "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";
    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders()
.AddDefaultUI();        

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Admin", policy => policy.RequireRole("Admin"));
    options.AddPolicy("Instructor", policy => policy.RequireRole("Instructor"));
    options.AddPolicy("Student", policy => policy.RequireRole("Student"));
});

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Identity/Account/Login";
    options.LogoutPath = "/Identity/Account/Logout";
    options.AccessDeniedPath = "/Identity/Account/AccessDenied";
    options.SlidingExpiration = true;
    options.ExpireTimeSpan = TimeSpan.FromDays(1);
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}
else
{
    app.UseDeveloperExceptionPage();      
}

app.UseHttpsRedirection();
app.UseStaticFiles();      

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();

        var canConnect = await context.Database.CanConnectAsync();
        if (!canConnect)
        {
            throw new InvalidOperationException(
                "Could not connect to the 'Nory' database. Make sure you've run " +
                "Nory_Database_Script.sql in SSMS against (localdb)\\mssqllocaldb " +
                "and that the connection string in appsettings.json is correct.");
        }

        var schemaExists = await context.Database
            .SqlQueryRaw<int>(
                "SELECT CASE WHEN OBJECT_ID('dbo.AspNetRoles', 'U') IS NOT NULL " +
                "AND OBJECT_ID('dbo.StoredImages', 'U') IS NOT NULL " +
                "AND COL_LENGTH('dbo.Lessons', 'ImageUrl') IS NOT NULL " +
                "AND COL_LENGTH('dbo.Quizzes', 'ImageUrl') IS NOT NULL " +
                "AND COL_LENGTH('dbo.ChordSongs', 'ImageUrl') IS NOT NULL " +
                "THEN 1 ELSE 0 END AS [Value]")
            .FirstOrDefaultAsync();
        if (schemaExists != 1)
        {
            throw new InvalidOperationException(
                "Connected to the 'Nory' database, but the application schema is incomplete. " +
                "Run the supplied database.sql script against the intended database (or the upgrade script for an existing database), " +
                "then restart the app.");
        }

        Console.WriteLine("Database connection and schema verified.");

        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

        
        
        string[] roleNames = { "Admin", "Instructor", "Student" };
        foreach (var roleName in roleNames)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                var roleResult = await roleManager.CreateAsync(new IdentityRole(roleName));
                if (!roleResult.Succeeded)
                    throw new InvalidOperationException(
                        $"Could not create role '{roleName}': " +
                        string.Join("; ", roleResult.Errors.Select(e => e.Description)));
            }
        }
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while migrating/seeding the database.");

        throw;
    }
}

app.MapGet("/references/MusicTheory.pdf", (IWebHostEnvironment environment) =>
{
    var pdfPath = Path.Combine(environment.ContentRootPath, "MusicTheory.pdf");
    return System.IO.File.Exists(pdfPath)
        ? Results.File(pdfPath, "application/pdf", enableRangeProcessing: true)
        : Results.NotFound();
});

app.MapGet("/references/open-chords.pdf", (IWebHostEnvironment environment) =>
{
    var pdfPath = Path.Combine(environment.ContentRootPath, "open-chords.pdf");
    return System.IO.File.Exists(pdfPath)
        ? Results.File(pdfPath, "application/pdf", enableRangeProcessing: true)
        : Results.NotFound();
});



app.MapGet("/media/{id:int}", async (int id, ApplicationDbContext db, HttpContext http) =>
{
    var image = await db.StoredImages.AsNoTracking()
        .Where(i => i.Id == id)
        .Select(i => new { i.Data, i.ContentType, i.UploadedAt })
        .FirstOrDefaultAsync();
    if (image is null) return Results.NotFound();

    
    
    http.Response.Headers.CacheControl = "private, max-age=86400";
    http.Response.Headers["X-Content-Type-Options"] = "nosniff";
    return Results.File(image.Data, image.ContentType,
        lastModified: new DateTimeOffset(DateTime.SpecifyKind(image.UploadedAt, DateTimeKind.Utc)));
}).RequireAuthorization();

app.MapRazorPages();

app.Run();
