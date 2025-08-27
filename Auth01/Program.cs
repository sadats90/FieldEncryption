using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using Auth01.Data;
using Auth01.Services;
using System.Security.Cryptography;

var builder = WebApplication.CreateBuilder(args);

// ----------------------
// Database setup
// ----------------------
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

// ----------------------
// Dependency Injection
// ----------------------
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddHttpClient(); // Required if any HTTP calls are used

// ----------------------
// Dummy Vault & Encryption Services
// ----------------------
builder.Services.AddSingleton<DummyVaultService>(new DummyVaultService("vault-keys.json"));
builder.Services.AddSingleton<EncryptionService>();

// ----------------------
// Authentication (Local Only)
// ----------------------
builder.Services.AddAuthentication("CustomAuth")
    .AddCookie("CustomAuth", options =>
    {
        options.LoginPath = "/Auth/Login";
        options.LogoutPath = "/Auth/Logout";
        options.Cookie.Name = "Auth01.Auth";
        options.ExpireTimeSpan = TimeSpan.FromHours(12);
        options.SlidingExpiration = true;
    });

// ----------------------
// MVC
// ----------------------
builder.Services.AddControllersWithViews();

var app = builder.Build();

// ----------------------
// Seed admin user
// ----------------------
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var authService = scope.ServiceProvider.GetRequiredService<IAuthService>();

    // Ensure database exists and migrations are applied
    try
    {
        context.Database.Migrate();
        Console.WriteLine("Database migrations applied successfully");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error applying migrations: {ex.Message}");
        // Fallback to EnsureCreated if migrations fail
        context.Database.EnsureCreated();
        Console.WriteLine("Database created using EnsureCreated");
    }

    // Seed admin
    var adminEmail = "admin@example.com";
    var adminUser = await authService.GetUserByEmailAsync(adminEmail);
    if (adminUser == null)
    {
        var admin = new Auth01.Models.User
        {
            FirstName = "Admin",
            LastName = "User",
            Email = adminEmail,
            Role = "Admin",
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        var success = await authService.CreateAdminUserAsync(admin, "Admin123!");
        Console.WriteLine(success
            ? $"Admin user created successfully: {adminEmail} / Admin123!"
            : "Failed to create admin user");
    }
    else
    {
        Console.WriteLine("Admin user already exists");
    }
}

// ----------------------
// Middleware pipeline
// ----------------------
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// ----------------------
// Routing
// ----------------------
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
