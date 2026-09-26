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
        // GET: Review
        // ==========================================

        public async Task<IActionResult> Index()
        {
            var reviews = await _reviewService.GetAllAsync();

            return View(reviews);
        }


        // ==========================================
        // GET: Review/Details/5
        // ==========================================

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
        // GET: Review/Create
        // ==========================================

        public IActionResult Create()
        {
            return View();
        }


        // ==========================================
        // POST: Review/Create
        // ==========================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Review review)
        {
            if (!ModelState.IsValid)
            {
                return View(review);
            }

            var result = await _reviewService.AddAsync(review);

            if (!result.Success)
            {
                ModelState.AddModelError("", result.Message);

                return View(review);
            }

            return RedirectToAction(nameof(Index));
        }


        // ==========================================
        // GET: Review/Edit/5
        // ==========================================

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

            return View(review);
        }


        // ==========================================
        // POST: Review/Edit/5
        // ==========================================

        [HttpPost]
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

            return RedirectToAction(nameof(Index));
        }


        // ==========================================
        // GET: Review/Delete/5
        // ==========================================

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

            return View(review);
        }


        // ==========================================
        // POST: Review/Delete/5
        // ==========================================

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            await _reviewService.DeleteAsync(id);

            return RedirectToAction(nameof(Index));
        }


        // ==========================================
        // GET: Review/VehicleReviews/5
        // ==========================================

        public async Task<IActionResult> VehicleReviews(int vehicleId)
        {
            var reviews = await _reviewService
                .GetByVehicleIdAsync(vehicleId);

            return View(reviews);
        }
    }
}