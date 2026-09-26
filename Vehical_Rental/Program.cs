using Microsoft.EntityFrameworkCore;
using Vehical_Rental.Data;
using Vehical_Rental.Repositories.Implementations;
using Vehical_Rental.Repositories.Interfaces;
using Vehical_Rental.Services.Implementations;
using Vehical_Rental.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
var builder = WebApplication.CreateBuilder(args);

// ======================================
// ADD SERVICES TO THE CONTAINER
// ======================================

builder.Services.AddControllersWithViews();
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";
    });

// ======================================
// DATABASE CONFIGURATION
// ======================================

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")
    ));


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


// Enable authorization
app.UseAuthentication();
app.UseAuthorization();

// Enable static files
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