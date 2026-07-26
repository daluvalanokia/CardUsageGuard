using CardUsageGuard.Data;
using CardUsageGuard.Middleware;
using CardUsageGuard.Models;
using CardUsageGuard.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Database — with retry on transient failures
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlOptions => sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 3,
            maxRetryDelay: TimeSpan.FromSeconds(5),
            errorNumbersToAdd: null)));

// Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 8;
    options.Password.RequireNonAlphanumeric = false;
    options.User.RequireUniqueEmail = true;
    options.SignIn.RequireConfirmedEmail = false; // Set to true for production
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();

// Google OAuth
builder.Services.AddAuthentication()
    .AddGoogle(options =>
    {
        options.ClientId = builder.Configuration["Authentication:Google:ClientId"] ?? "PLACEHOLDER_CLIENT_ID";
        options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"] ?? "PLACEHOLDER_CLIENT_SECRET";
    });

// Services (DI)
builder.Services.AddScoped<OtpService>();
builder.Services.AddScoped<CardStatusService>();
builder.Services.AddScoped<AuditLogService>();
builder.Services.AddHttpClient<ProviderApiService>();

// MVC
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

// Session for OTP flow
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

// Ensure database exists and seed — safe to run on every startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    try
    {
        // Create the database + tables if they don't exist yet.
        // This is a quick-start shortcut — use 'dotnet ef migrations' for proper schema management.
        await db.Database.EnsureCreatedAsync();

        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        await SeedData.InitializeAsync(roleManager, userManager);
    }
    catch (Exception ex)
    {
        Console.WriteLine("[WARNING] Database setup skipped.");
        Console.WriteLine($"[WARNING] Run migrations: dotnet ef migrations add InitialCreate && dotnet ef database update");
        Console.WriteLine($"[WARNING] Error: {ex.Message}");
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<AuditMiddleware>();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();

app.Run();
