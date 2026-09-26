using Microsoft.AspNetCore.Mvc;
using Vehical_Rental.Models;
using Vehical_Rental.Services.Interfaces;

namespace Vehical_Rental.Controllers
{
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

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _userService.GetByIdAsync(id.Value);

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
        public async Task<IActionResult> Create(User user)
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

            return RedirectToAction(nameof(Index));
        }


        // ==========================================
        // GET: User/Edit/5
        // ==========================================

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _userService.GetByIdAsync(id.Value);

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
        public async Task<IActionResult> Edit(int id, User user)
        {
            if (id != user.Id)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                return View(user);
            }

            await _userService.UpdateAsync(user);

            return RedirectToAction(nameof(Index));
        }


        // ==========================================
        // GET: User/Delete/5
        // ==========================================

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _userService.GetByIdAsync(id.Value);

            if (user == null)
            {
                return NotFound();
            }

            return View(user);
        }


        // ==========================================
        // POST: User/Delete/5
        // ==========================================

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            await _userService.DeleteAsync(id);

            return RedirectToAction(nameof(Index));
        }
    }
}