using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Vehical_Rental.Models;
using Vehical_Rental.Services.Interfaces;

namespace Vehical_Rental.Controllers
{
    public class ReviewController : Controller
    {
        private readonly IReviewService _reviewService;

        public ReviewController(IReviewService reviewService)
        {
            _reviewService = reviewService;
        }

        // ==========================================
        // GET: Review (Public - PDF 1 Slide 27)
        // ==========================================
        [AllowAnonymous]
        public async Task<IActionResult> Index()
        {
            var reviews = await _reviewService.GetAllAsync();
            return View(reviews);
        }

        // ==========================================
        // GET: Review/Details/5 (Public - PDF 1 Slide 27)
        // ==========================================
        [AllowAnonymous]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var review = await _reviewService.GetByIdAsync(id.Value);
            if (review == null)
            {
                return NotFound();
            }

            return View(review);
        }

        // ==========================================
        // GET: Review/Create (Protected - PDF 1 Slide 26)
        // ==========================================
        [Authorize]
        public IActionResult Create(int? vehicleId = null)
        {
            if (vehicleId.HasValue)
            {
                ViewBag.SelectedVehicleId = vehicleId.Value;
            }
            return View();
        }

        // ==========================================
        // POST: Review/Create (Protected)
        // ==========================================
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Review review)
        {
            // If normal authenticated user, bind their current user id
            if (!User.IsInRole("Admin"))
            {
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!string.IsNullOrEmpty(currentUserId))
                {
                    review.UserId = currentUserId;
                }
            }

            if (!ModelState.IsValid)
            {
                return View(review);
            }

            var result = await _reviewService.AddAsync(review);

            if (!result.Success)
            {
                ModelState.AddModelError(string.Empty, result.Message);
                return View(review);
            }

            TempData["Success"] = "Thank you! Your review has been submitted.";
            return RedirectToAction(nameof(Index));
        }

        // ==========================================
        // GET: Review/Edit/5 (Protected)
        // ==========================================
        [Authorize]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var review = await _reviewService.GetByIdAsync(id.Value);
            if (review == null)
            {
                return NotFound();
            }

            if (!User.IsInRole("Admin"))
            {
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (review.UserId != currentUserId)
                {
                    return Forbid();
                }
            }

            return View(review);
        }

        // ==========================================
        // POST: Review/Edit/5 (Protected)
        // ==========================================
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Review review)
        {
            if (id != review.Id)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                return View(review);
            }

            await _reviewService.UpdateAsync(review);
            TempData["Success"] = "Review updated successfully.";

            return RedirectToAction(nameof(Index));
        }

        // ==========================================
        // GET: Review/Delete/5 (Protected)
        // ==========================================
        [Authorize]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var review = await _reviewService.GetByIdAsync(id.Value);
            if (review == null)
            {
                return NotFound();
            }

            if (!User.IsInRole("Admin"))
            {
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (review.UserId != currentUserId)
                {
                    return Forbid();
                }
            }

            return View(review);
        }

        // ==========================================
        // POST: Review/Delete/5 (Protected)
        // ==========================================
        [HttpPost, ActionName("Delete")]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            await _reviewService.DeleteAsync(id);
            TempData["Success"] = "Review deleted successfully.";

            return RedirectToAction(nameof(Index));
        }

        // ==========================================
        // GET: Review/VehicleReviews/5 (Public)
        // ==========================================
        [AllowAnonymous]
        public async Task<IActionResult> VehicleReviews(int vehicleId)
        {
            var reviews = await _reviewService.GetByVehicleIdAsync(vehicleId);
            return View(reviews);
        }
    }
}