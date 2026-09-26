using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
        // GET: Admin
        // ==========================================

        public async Task<IActionResult> Index()
        {
            var users = await _userService.GetAllAsync();
            var vehicles = await _vehicleService.GetAllAsync();
            var bookings = await _bookingService.GetAllAsync();
            var reviews = await _reviewService.GetAllAsync();

            ViewBag.TotalUsers = users.Count();
            ViewBag.TotalVehicles = vehicles.Count();
            ViewBag.TotalBookings = bookings.Count();
            ViewBag.TotalReviews = reviews.Count();

            return View();
        }
    }
}