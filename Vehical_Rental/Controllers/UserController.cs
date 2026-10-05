using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Vehical_Rental.Models;
using Vehical_Rental.Services.Interfaces;

namespace Vehical_Rental.Controllers
{
    [Authorize(Roles = "Admin")]
    public class UserController : Controller
    {
        private readonly IUserService _userService;

        public UserController(IUserService userService)
        {
            _userService = userService;
        }

        // ==========================================
        // GET: User
        // ==========================================
        public async Task<IActionResult> Index()
        {
            var users = await _userService.GetAllAsync();
            return View(users);
        }

        // ==========================================
        // GET: User/Details/5
        // ==========================================
        public async Task<IActionResult> Details(string? id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var user = await _userService.GetByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            return View(user);
        }

        // ==========================================
        // GET: User/Create
        // ==========================================
        public IActionResult Create()
        {
            return View();
        }

        // ==========================================
        // POST: User/Create
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ApplicationUser user, string password, string role = "Customer")
        {
            if (string.IsNullOrWhiteSpace(password) || password.Length < 6)
            {
                ModelState.AddModelError("PasswordHash", "Password must be at least 6 characters.");
            }

            if (!ModelState.IsValid)
            {
                return View(user);
            }

            var result = await _userService.RegisterAsync(user, password, role);
            if (!result.Success)
            {
                ModelState.AddModelError(string.Empty, result.Message);
                return View(user);
            }

            TempData["Success"] = "User created successfully.";
            return RedirectToAction(nameof(Index));
        }

        // ==========================================
        // GET: User/Edit/5
        // ==========================================
        public async Task<IActionResult> Edit(string? id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var user = await _userService.GetByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            return View(user);
        }

        // ==========================================
        // POST: User/Edit/5
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, ApplicationUser user, string role = "Customer")
        {
            if (id != user.Id)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                return View(user);
            }

            var result = await _userService.UpdateAsync(user, role);
            if (!result.Success)
            {
                ModelState.AddModelError(string.Empty, result.Message);
                return View(user);
            }

            TempData["Success"] = "User updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        // ==========================================
        // GET: User/Delete/5
        // ==========================================
        public async Task<IActionResult> Delete(string? id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var user = await _userService.GetByIdAsync(id);
            if (user == null)
            {
                return NotFound();
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

        // ==========================================
        // POST: User/Delete/5
        // ==========================================
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id, string? returnUrl = null)
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

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction("Users", "Admin");
        }
    }
}