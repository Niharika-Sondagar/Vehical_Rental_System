using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Vehical_Rental.Models;

namespace Vehical_Rental.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
        }

        // ==========================================
        // GET: Account/Register
        // ==========================================
        [AllowAnonymous]
        public IActionResult Register()
        {
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Home");
            }

            return View();
        }

        // ==========================================
        // POST: Account/Register
        // Follows PDF 2 Slide 14 registration flow:
        // Register.cshtml -> InputModel -> ApplicationUser -> UserManager -> Identity -> AspNetUsers
        // ==========================================
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var normalizedEmail = model.Email.Trim().ToLowerInvariant();

            // Check for duplicate email registration
            var existingUser = await _userManager.FindByEmailAsync(normalizedEmail);
            if (existingUser != null)
            {
                ModelState.AddModelError("Email", "An account with this email address already exists. Please sign in or use another email.");
                return View(model);
            }

            // Create custom ApplicationUser extending IdentityUser (PDF 2)
            var user = new ApplicationUser
            {
                UserName = normalizedEmail,
                Email = normalizedEmail,
                FullName = model.FullName.Trim(),
                PhoneNumber = model.PhoneNumber?.Trim() ?? string.Empty,
                Address = model.Address?.Trim(),
                CreatedAt = DateTime.UtcNow
            };

            // UserManager handles password hashing and Identity storage (PDF 1 & 2)
            var result = await _userManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
            {
                // Ensure chosen role ("VehicleOwner" or "Customer") exists and assign it
                var chosenRole = model.Role == "VehicleOwner" ? "VehicleOwner" : "Customer";
                if (!await _roleManager.RoleExistsAsync(chosenRole))
                {
                    await _roleManager.CreateAsync(new IdentityRole(chosenRole));
                }

                await _userManager.AddToRoleAsync(user, chosenRole);

                // Sign in the user with SignInManager (creating authentication cookie)
                await _signInManager.SignInAsync(user, isPersistent: false);

                if (chosenRole == "VehicleOwner")
                {
                    TempData["Success"] = $"Welcome to RentDrive, {user.FullName}! Your Vehicle Owner account is ready. You can now list and manage your vehicles.";
                    return RedirectToAction("MyVehicles", "Vehicle");
                }

                TempData["Success"] = "Welcome to RentDrive! Your account has been created successfully.";
                return RedirectToAction("Index", "Home");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }

        // ==========================================
        // GET: Account/Login
        // ==========================================
        [AllowAnonymous]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Home");
            }

            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        // ==========================================
        // POST: Account/Login
        // Handles authentication via SignInManager (PDF 1 Slides 21-23)
        // ==========================================
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Find user by email first
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "Invalid login credentials. Please check your email and password.");
                return View(model);
            }

            // Check if user is locked out by Administrator for system protection
            if (await _userManager.IsLockedOutAsync(user))
            {
                ModelState.AddModelError(string.Empty, "This account has been suspended by the System Administrator for platform protection. Please contact support.");
                return View(model);
            }

            // SignInManager verifies password hash and creates authentication cookie
            var result = await _signInManager.PasswordSignInAsync(
                user.UserName!,
                model.Password,
                model.RememberMe,
                lockoutOnFailure: true
            );

            if (result.Succeeded)
            {
                TempData["Success"] = $"Welcome back, {user.FullName}!";

                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                {
                    return Redirect(returnUrl);
                }

                if (await _userManager.IsInRoleAsync(user, "Admin"))
                {
                    return RedirectToAction("Index", "Admin");
                }

                if (await _userManager.IsInRoleAsync(user, "VehicleOwner"))
                {
                    return RedirectToAction("MyVehicles", "Vehicle");
                }

                return RedirectToAction("Index", "Home");
            }

            if (result.IsLockedOut)
            {
                ModelState.AddModelError(string.Empty, "This account is suspended or locked out for platform protection.");
                return View(model);
            }

            ModelState.AddModelError(string.Empty, "Invalid login credentials. Please check your email and password.");
            return View(model);
        }

        // ==========================================
        // POST: Account/Logout
        // Handles sign-out via SignInManager (PDF 1 Slide 22)
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            TempData["Info"] = "You have been successfully signed out.";
            return RedirectToAction("Index", "Home");
        }

        // ==========================================
        // GET: Account/Logout
        // Allows direct sign-out via link or GET navigation
        // ==========================================
        [HttpGet]
        [ActionName("Logout")]
        [Authorize]
        public async Task<IActionResult> LogoutGet()
        {
            await _signInManager.SignOutAsync();
            TempData["Info"] = "You have been successfully signed out.";
            return RedirectToAction("Index", "Home");
        }

        // ==========================================
        // GET: Account/AccessDenied
        // ==========================================
        [AllowAnonymous]
        public IActionResult AccessDenied()
        {
            return View();
        }

        // ==========================================
        // GET: Account/Profile
        // ==========================================
        [Authorize]
        public async Task<IActionResult> Profile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound("User profile could not be loaded.");
            }

            var roles = await _userManager.GetRolesAsync(user);
            user.Role = roles.FirstOrDefault() ?? "Customer";

            return View(user);
        }

        // ==========================================
        // GET: Account/EditProfile
        // ==========================================
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> EditProfile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound("User profile could not be loaded.");
            }

            var roles = await _userManager.GetRolesAsync(user);
            var model = new EditProfileViewModel
            {
                FullName = user.FullName,
                Email = user.Email ?? string.Empty,
                PhoneNumber = user.PhoneNumber ?? string.Empty,
                Address = user.Address,
                Role = roles.FirstOrDefault() ?? "Customer"
            };

            return View(model);
        }

        // ==========================================
        // POST: Account/EditProfile
        // ==========================================
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProfile(EditProfileViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound("User profile could not be loaded.");
            }

            var roles = await _userManager.GetRolesAsync(user);
            model.Role = roles.FirstOrDefault() ?? "Customer";
            model.Email = user.Email ?? string.Empty;

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            user.FullName = model.FullName.Trim();
            user.PhoneNumber = model.PhoneNumber.Trim();
            user.Address = model.Address?.Trim();

            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded)
            {
                TempData["Success"] = "Your profile has been updated successfully.";
                return RedirectToAction(nameof(Profile));
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }
    }
}