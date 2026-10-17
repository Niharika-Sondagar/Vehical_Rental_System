using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Vehical_Rental.Models;
using Vehical_Rental.Services.Interfaces;

namespace Vehical_Rental.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly IUserService _userService;
        private readonly IVehicleService _vehicleService;
        private readonly IBookingService _bookingService;
        private readonly IReviewService _reviewService;

        public AdminController(
            IUserService userService,
            IVehicleService vehicleService,
            IBookingService bookingService,
            IReviewService reviewService)
        {
            _userService = userService;
            _vehicleService = vehicleService;
            _bookingService = bookingService;
            _reviewService = reviewService;
        }

        // ==========================================
        // 1. SYSTEM OVERVIEW & EXECUTIVE DASHBOARD
        // ==========================================
        public async Task<IActionResult> Index()
        {
            var users = (await _userService.GetAllAsync()).ToList();
            var vehicles = (await _vehicleService.GetAllAsync()).ToList();
            var bookings = (await _bookingService.GetAllAsync()).ToList();
            var reviews = (await _reviewService.GetAllAsync()).ToList();

            ViewBag.TotalUsers = users.Count;
            ViewBag.TotalCustomers = users.Count(u => u.Role == "Customer");
            ViewBag.TotalOwners = users.Count(u => u.Role == "VehicleOwner");
            ViewBag.TotalAdmins = users.Count(u => u.Role == "Admin");

            ViewBag.TotalVehicles = vehicles.Count;
            ViewBag.AvailableVehicles = vehicles.Count(v => v.IsAvailable);
            ViewBag.RentedVehicles = vehicles.Count(v => !v.IsAvailable);

            ViewBag.TotalBookings = bookings.Count;
            ViewBag.ConfirmedBookings = bookings.Count(b => b.Status == "Confirmed" || b.Status == "Completed");
            ViewBag.TotalRevenue = bookings.Where(b => b.Status == "Confirmed" || b.Status == "Completed").Sum(b => b.TotalPrice);

            ViewBag.TotalReviews = reviews.Count;
            ViewBag.AverageRating = reviews.Any() ? Math.Round(reviews.Average(r => r.Rating), 1) : 5.0;

            // Security & Protection summary
            var lockedUsers = 0;
            foreach (var user in users)
            {
                if (await _userService.IsLockedOutAsync(user))
                {
                    lockedUsers++;
                }
            }
            ViewBag.SuspendedUsersCount = lockedUsers;

            return View();
        }

        // ==========================================
        // 2. SYSTEM ANALYSIS (Fleet, Revenue, Utilization, Distribution)
        // ==========================================
        public async Task<IActionResult> Dashboard()
        {
            var users = (await _userService.GetAllAsync()).ToList();
            var vehicles = (await _vehicleService.GetAllAsync()).ToList();
            var bookings = (await _bookingService.GetAllAsync()).ToList();
            var reviews = (await _reviewService.GetAllAsync()).ToList();

            ViewBag.TotalUsers = users.Count;
            ViewBag.TotalCustomers = users.Count(u => u.Role == "Customer");
            ViewBag.TotalOwners = users.Count(u => u.Role == "VehicleOwner");

            ViewBag.TotalVehicles = vehicles.Count;
            ViewBag.AvailableVehicles = vehicles.Count(v => v.IsAvailable);
            ViewBag.RentedVehicles = vehicles.Count(v => !v.IsAvailable);
            ViewBag.UtilizationRate = vehicles.Any() ? Math.Round((double)vehicles.Count(v => !v.IsAvailable) / vehicles.Count * 100, 1) : 0;

            ViewBag.GrossRevenue = bookings.Where(b => b.Status != "Cancelled").Sum(b => b.TotalPrice);
            ViewBag.CompletedRevenue = bookings.Where(b => b.Status == "Completed").Sum(b => b.TotalPrice);
            ViewBag.AverageBookingValue = bookings.Any(b => b.Status != "Cancelled") 
                ? Math.Round(bookings.Where(b => b.Status != "Cancelled").Average(b => b.TotalPrice), 2) 
                : 0m;

            return View();
        }

        // ==========================================
        // 3. SYSTEM PROTECTION & SECURITY CENTER
        // Manage user suspensions, vehicle safety status, system audit
        // ==========================================
        public async Task<IActionResult> Protection()
        {
            var users = (await _userService.GetAllAsync()).ToList();
            var vehicles = (await _vehicleService.GetAllAsync()).ToList();

            var userSecurityList = new List<(ApplicationUser User, bool IsLockedOut)>();
            foreach (var u in users)
            {
                var isLocked = await _userService.IsLockedOutAsync(u);
                userSecurityList.Add((u, isLocked));
            }

            ViewBag.UserSecurityList = userSecurityList;
            ViewBag.SuspendedUsersCount = userSecurityList.Count(x => x.IsLockedOut);
            ViewBag.SuspendedVehiclesCount = vehicles.Count(v => !v.IsAvailable);

            return View(vehicles);
        }

        // ==========================================
        // POST: Admin/ToggleUserLockout (System Protection Action)
        // Suspends or restores account to protect against fraud/abuse
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleUserLockout(string id, string? returnAction = null)
        {
            var result = await _userService.ToggleLockoutAsync(id);
            if (result.Success)
            {
                TempData["Success"] = result.Message;
            }
            else
            {
                TempData["Error"] = result.Message;
            }

            if (returnAction == "Users")
            {
                return RedirectToAction(nameof(Users));
            }

            return RedirectToAction(nameof(Protection));
        }

        // ==========================================
        // POST: Admin/ToggleVehicleSafety (System Protection Action)
        // Admin can suspend or restore a vehicle listing for passenger safety
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleVehicleSafety(int id, string? returnAction = null)
        {
            var vehicle = await _vehicleService.GetByIdAsync(id);
            if (vehicle == null)
            {
                TempData["Error"] = "Vehicle not found.";
                return RedirectToAction(nameof(Protection));
            }

            vehicle.IsAvailable = !vehicle.IsAvailable;
            await _vehicleService.UpdateAsync(vehicle);

            if (!vehicle.IsAvailable)
            {
                TempData["Success"] = $"Vehicle '{vehicle.FullName}' was suspended from the platform for safety protection.";
            }
            else
            {
                TempData["Success"] = $"Vehicle '{vehicle.FullName}' was restored and is now available for rent.";
            }

            if (returnAction == "Vehicles")
            {
                return RedirectToAction(nameof(Vehicles));
            }

            return RedirectToAction(nameof(Protection));
        }

        // ==========================================
        // 4. FLEET ANALYSIS & OVERSIGHT (Admin Analyzes Fleet)
        // Admin views all vehicles and their owners (Add/Edit done by Owner)
        // ==========================================
        public async Task<IActionResult> Vehicles()
        {
            var vehicles = await _vehicleService.GetAllAsync();
            return View(vehicles);
        }

        // ==========================================
        // 5. USER SECURITY & DIRECTORY
        // Admin oversees all accounts and role assignments
        // ==========================================
        public async Task<IActionResult> Users()
        {
            var users = (await _userService.GetAllAsync()).ToList();

            var userSecurityList = new List<(ApplicationUser User, bool IsLockedOut)>();
            foreach (var u in users)
            {
                var isLocked = await _userService.IsLockedOutAsync(u);
                userSecurityList.Add((u, isLocked));
            }

            ViewBag.UserSecurityList = userSecurityList;
            return View(users);
        }

        // ==========================================
        // 6. RESERVATION OPERATIONS
        // ==========================================
        public async Task<IActionResult> Bookings()
        {
            var bookings = await _bookingService.GetAllAsync();
            return View(bookings);
        }

        // ==========================================
        // 7. FINANCIAL & OPERATIONAL REPORTS
        // ==========================================
        public async Task<IActionResult> Reports()
        {
            return View();
        }

        // ==========================================
        // 8. REMOVE USER FROM SYSTEM (ADMIN FUNCTIONALITY)
        // Confirmation review & permanent removal
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> DeleteUser(string? id)
        {
            if (string.IsNullOrEmpty(id))
            {
                TempData["Error"] = "No user specified for removal.";
                return RedirectToAction(nameof(Users));
            }

            var user = await _userService.GetByIdAsync(id);
            if (user == null)
            {
                TempData["Error"] = "User account not found.";
                return RedirectToAction(nameof(Users));
            }

            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var isSelf = !string.IsNullOrEmpty(currentUserId) && string.Equals(currentUserId, user.Id, StringComparison.OrdinalIgnoreCase);

            var isLastAdmin = false;
            if (user.Role == "Admin")
            {
                var allUsers = await _userService.GetAllAsync();
                isLastAdmin = allUsers.Count(u => u.Role == "Admin") <= 1;
            }

            ViewBag.IsSelf = isSelf;
            ViewBag.IsLastAdmin = isLastAdmin;
            ViewBag.ActiveBookingsCount = user.Bookings?.Count(b => b.Status == "Confirmed" || b.Status == "Pending") ?? 0;
            ViewBag.OwnedVehiclesCount = user.OwnedVehicles?.Count ?? 0;
            ViewBag.ReviewsCount = user.Reviews?.Count ?? 0;

            return View(user);
        }

        [HttpPost]
        [ActionName("DeleteUser")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUserConfirmed(string id, string? returnAction = null)
        {
            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var result = await _userService.DeleteAsync(id, currentUserId);

            if (result.Success)
            {
                TempData["Success"] = result.Message;
            }
            else
            {
                TempData["Error"] = result.Message;
            }

            if (returnAction == "Protection")
            {
                return RedirectToAction(nameof(Protection));
            }

            return RedirectToAction(nameof(Users));
        }
    }
}