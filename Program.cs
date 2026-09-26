using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using NoryMusicLMS_VS.Data;
using NoryMusicLMS_VS.Models;
using NoryMusicLMS_VS.Services;

    var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages(options =>
{
    options.Conventions.AuthorizeAreaFolder("Admin", "/", "Admin");
    options.Conventions.AuthorizeAreaFolder("Instructor", "/", "Instructor");
    options.Conventions.AuthorizeAreaFolder("Student", "/", "Student");
});

// Configure database connection (using LocalDB for simplicity)
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddScoped<AwardService>();

// Add Identity with roles
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    // Password settings
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 6;
    options.Password.RequiredUniqueChars = 1;

    // Lockout settings
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;

    // User settings
    options.User.AllowedUserNameCharacters =
        "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";
    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders()
.AddDefaultUI(); // Uses built-in Identity pages (login, register, etc.)

// Configure authorization policies for role-based Razor Area access
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Admin", policy => policy.RequireRole("Admin"));
    options.AddPolicy("Instructor", policy => policy.RequireRole("Instructor"));
    options.AddPolicy("Student", policy => policy.RequireRole("Student"));
});

// Configure cookies
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Identity/Account/Login";
    options.LogoutPath = "/Identity/Account/Logout";
    options.AccessDeniedPath = "/Identity/Account/AccessDenied";
    options.SlidingExpiration = true;
    options.ExpireTimeSpan = TimeSpan.FromDays(1);
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios.
    app.UseHsts();
}
else
{
    app.UseDeveloperExceptionPage(); // Show detailed errors in development
}

app.UseHttpsRedirection();
app.UseStaticFiles(); // For wwwroot (CSS, JS, images)

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// Seeds roles/admin user on startup. Schema is NOT managed by EF migrations
// here - the "Nory" database schema is created once, manually, by running
// Nory_Database_Script.sql in SSMS (see NORY_DATABASE_SETUP.md). This avoids
// the migration-generation issues we hit earlier; it does mean that if you
// change a model (add/remove a property, add a new entity) you must update
// Nory_Database_Script.sql and re-run it (or hand-write the matching
// ALTER TABLE) yourself - nothing here will do that automatically.
//
// NOTE: this used to call context.Database.EnsureDeleted() before Migrate(),
// which wiped the entire "Nory" database every single time the app started -
// that was the main cause of "lost data" / broken login problems. Removed.
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();

        // Just confirm we can actually reach the database and that the
        // schema has been created (via the SQL script). We deliberately do
        // NOT call context.Database.Migrate() or EnsureCreated() here.
        var canConnect = await context.Database.CanConnectAsync();
        if (!canConnect)
        {
            throw new InvalidOperationException(
                "Could not connect to the 'Nory' database. Make sure you've run " +
                "Nory_Database_Script.sql in SSMS against (localdb)\\mssqllocaldb " +
                "and that the connection string in appsettings.json is correct.");
        }

        // Cheap check that the schema actually exists yet (not just that the
        // server/database itself is reachable) - gives a clear error message
        // instead of a confusing SqlException from deeper inside Identity.
        var schemaExists = await context.Database
            .SqlQueryRaw<int>("SELECT CASE WHEN OBJECT_ID('dbo.AspNetRoles', 'U') IS NOT NULL THEN 1 ELSE 0 END AS [Value]")
            .FirstOrDefaultAsync();
        if (schemaExists != 1)
        {
            throw new InvalidOperationException(
                "Connected to the 'Nory' database, but its tables don't exist yet. " +
                "Run Nory_Database_Script.sql in SSMS against (localdb)\\mssqllocaldb first, " +
                "then restart the app.");
        }

        Console.WriteLine("Database connection and schema verified.");

        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

        // Create roles if they don't exist
        string[] roleNames = { "Admin", "Instructor", "Student" };
        foreach (var roleName in roleNames)
        {
            var roleExists = await roleManager.RoleExistsAsync(roleName);
            if (!roleExists)
            {
                var roleResult = await roleManager.CreateAsync(new IdentityRole(roleName));
                if (!roleResult.Succeeded)
                    throw new InvalidOperationException(
                        $"Could not create role '{roleName}': " +
                        string.Join("; ", roleResult.Errors.Select(e => e.Description)));
            }
        }

        // Create admin user if it doesn't exist
        var adminEmail = "admin@norymusic.com";
        var adminUser = await userManager.FindByEmailAsync(adminEmail);
        if (adminUser == null)
        {
            adminUser = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                FirstName = "Nory",
                LastName = "Administrator",
                EmailConfirmed = true,
                PhoneNumber = "+1234567890"
            };

            var result = await userManager.CreateAsync(adminUser, "Admin@123");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, "Admin");
            }
        }
        else if (!await userManager.IsInRoleAsync(adminUser, "Admin"))
        {
            var roleResult = await userManager.AddToRoleAsync(adminUser, "Admin");
            if (!roleResult.Succeeded)
                throw new InvalidOperationException(string.Join("; ", roleResult.Errors.Select(e => e.Description)));
        }

        // Create a demo instructor account if it doesn't exist
        var instructorEmail = "instructor@norymusic.com";
        var instructorUser = await userManager.FindByEmailAsync(instructorEmail);
        if (instructorUser == null)
        {
            instructorUser = new ApplicationUser
            {
                UserName = instructorEmail,
                Email = instructorEmail,
                FirstName = "Julian",
                LastName = "Vance",
                EmailConfirmed = true,
                PhoneNumber = "+1234567891"
            };

            var result = await userManager.CreateAsync(instructorUser, "Instructor@123");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(instructorUser, "Instructor");
            }
        }
        else if (!await userManager.IsInRoleAsync(instructorUser, "Instructor"))
        {
            var roleResult = await userManager.AddToRoleAsync(instructorUser, "Instructor");
            if (!roleResult.Succeeded)
                throw new InvalidOperationException(string.Join("; ", roleResult.Errors.Select(e => e.Description)));
        }

        // Create a demo student account if it doesn't exist
        var studentEmail = "student@norymusic.com";
        var studentUser = await userManager.FindByEmailAsync(studentEmail);
        if (studentUser == null)
        {
            studentUser = new ApplicationUser
            {
                UserName = studentEmail,
                Email = studentEmail,
                FirstName = "Maya",
                LastName = "Lin",
                EmailConfirmed = true,
                PhoneNumber = "+1234567892"
            };

            var result = await userManager.CreateAsync(studentUser, "Student@123");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(studentUser, "Student");
            }
        }
        else if (!await userManager.IsInRoleAsync(studentUser, "Student"))
        {
            var roleResult = await userManager.AddToRoleAsync(studentUser, "Student");
            if (!roleResult.Succeeded)
                throw new InvalidOperationException(string.Join("; ", roleResult.Errors.Select(e => e.Description)));
        }
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while migrating/seeding the database.");

        // Surface the error during development instead of silently swallowing it -
        // a silent failure here is what made past connection/authentication
        // problems look like "the app just doesn't work" with no clear reason.
        if (app.Environment.IsDevelopment())
        {
            throw;
        }
    }
}

// Serve only the supplied music-theory PDF from the project root. Keep the
// project root itself out of the general static-file provider.
app.MapGet("/references/MusicTheory.pdf", (IWebHostEnvironment environment) =>
{
    var pdfPath = Path.Combine(environment.ContentRootPath, "MusicTheory.pdf");
    return System.IO.File.Exists(pdfPath)
        ? Results.File(pdfPath, "application/pdf", enableRangeProcessing: true)
        : Results.NotFound();
});

app.MapRazorPages();

app.Run();
