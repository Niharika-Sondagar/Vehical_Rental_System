using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Vehical_Rental.Models;
using Vehical_Rental.Services.Interfaces;

namespace Vehical_Rental.Controllers
{
    public class AccountController : Controller
    {
        private readonly IUserService _userService;

        public AccountController(IUserService userService)
        {
            _userService = userService;
        }


        // ==========================================
        // GET: Account/Register
        // ==========================================

        public IActionResult Register()
        {
            return View();
        }


        // ==========================================
        // POST: Account/Register
        // ==========================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(User user)
        {
            if (!ModelState.IsValid)
            {
                return View(user);
            }

            var result = await _userService.RegisterAsync(user);

            if (!result.Success)
            {
                ModelState.AddModelError("", result.Message);

                return View(user);
            }

            return RedirectToAction(nameof(Login));
        }


        // ==========================================
        // GET: Account/Login
        // ==========================================

        public IActionResult Login()
        {
            return View();
        }


        // ==========================================
        // POST: Account/Login
        // ==========================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(
            string email,
            string password)
        {
            if (string.IsNullOrWhiteSpace(email) ||
                string.IsNullOrWhiteSpace(password))
            {
                ModelState.AddModelError(
                    "",
                    "Email and password are required."
                );

                return View();
            }

            var user = await _userService.LoginAsync(
                email,
                password
            );

            if (user == null)
            {
                ModelState.AddModelError(
                    "",
                    "Invalid email or password."
                );

                return View();
            }


            // ==========================================
            // CREATE USER CLAIMS
            // ==========================================

            var claims = new List<Claim>
            {
                new Claim(
                    ClaimTypes.NameIdentifier,
                    user.Id.ToString()
                ),

                new Claim(
                    ClaimTypes.Name,
                    user.FullName
                ),

                new Claim(
                    ClaimTypes.Email,
                    user.Email
                ),

                new Claim(
                    ClaimTypes.Role,
                    user.Role
                )
            };


            var claimsIdentity = new ClaimsIdentity(
                claims,
                CookieAuthenticationDefaults.AuthenticationScheme
            );


            var authProperties = new AuthenticationProperties
            {
                IsPersistent = true
            };


            // ==========================================
            // SIGN IN
            // ==========================================

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties
            );


            // ==========================================
            // REDIRECT BASED ON ROLE
            // ==========================================

            if (user.Role == "Admin")
            {
                return RedirectToAction(
                    "Index",
                    "Admin"
                );
            }

            return RedirectToAction(
                "Index",
                "Home"
            );
        }


        // ==========================================
        // POST: Account/Logout
        // ==========================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(
                CookieAuthenticationDefaults.AuthenticationScheme
            );

            return RedirectToAction(
                "Index",
                "Home"
            );
        }


        // ==========================================
        // GET: Account/AccessDenied
        // ==========================================

        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}