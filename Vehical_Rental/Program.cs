using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Vehical_Rental.Data;
using Vehical_Rental.Models;
using Vehical_Rental.Repositories.Implementations;
using Vehical_Rental.Repositories.Interfaces;
using Vehical_Rental.Services.Implementations;
using Vehical_Rental.Services.Interfaces;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    });

builder.Services.AddHttpClient();

// ======================================
// DATABASE CONFIGURATION
// ======================================
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlOptions =>
        {
            sqlOptions.EnableRetryOnFailure();
        }));

// ======================================
// ASP.NET CORE IDENTITY CONFIGURATION
// Matching specifications in PDF 1 & PDF 2
// ======================================
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    // Sign-in settings
    options.SignIn.RequireConfirmedAccount = false;

    // Password requirements as specified in PDF 2 Slide 10
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 6;

    // User options
    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// Configure cookie authentication (Login / AccessDenied paths)
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromDays(14);
    options.SlidingExpiration = true;
});

// ======================================
// REPOSITORIES
// ======================================

builder.Services.AddScoped<IVehicleRepository, VehicleRepository>();
builder.Services.AddScoped<IBookingRepository, BookingRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IReviewRepository, ReviewRepository>();

// ======================================
// SERVICES
// ======================================

builder.Services.AddScoped<IVehicleService, VehicleService>();
builder.Services.AddScoped<IBookingService, BookingService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IReviewService, ReviewService>();

// ======================================
// BUILD APPLICATION
// ======================================

var app = builder.Build();

// ======================================
// INITIALIZE & SEED IDENTITY DATABASE
// ======================================
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        await DbInitializer.InitializeAsync(services);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while seeding the database with Identity.");
    }
}

// ======================================
// HTTP REQUEST PIPELINE
// ======================================

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// Redirect HTTP to HTTPS
app.UseHttpsRedirection();

// Enable routing
app.UseRouting();

// Enable Authentication & Authorization middleware
app.UseAuthentication();
app.UseAuthorization();

// Enable static files from wwwroot
app.UseStaticFiles();
app.MapStaticAssets();

// ======================================
// DEFAULT ROUTE
// ======================================

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}"
)
.WithStaticAssets();

// ======================================
// RUN APPLICATION
// ======================================

app.Run();