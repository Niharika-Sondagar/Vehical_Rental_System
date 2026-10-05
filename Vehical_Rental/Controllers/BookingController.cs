using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Vehical_Rental.Models;
using Vehical_Rental.Services.Interfaces;

namespace Vehical_Rental.Controllers
{
    [Authorize]
    public class BookingController : Controller
    {
        private readonly IBookingService _bookingService;

        public BookingController(IBookingService bookingService)
        {
            _bookingService = bookingService;
        }

        // ==========================================
        // GET: Booking (Role-Aware Dispatcher)
        // ==========================================
        public async Task<IActionResult> Index()
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Vehicle Owners are routed to their dedicated Booking Requests inbox
            if (User.IsInRole("VehicleOwner"))
            {
                return RedirectToAction(nameof(Requests));
            }

            // Admins oversee all system bookings
            if (User.IsInRole("Admin"))
            {
                var allBookings = await _bookingService.GetAllAsync();
                return View(allBookings);
            }

            // Customers only see their own rental requests and bookings
            var customerBookings = await _bookingService.GetByCustomerIdAsync(currentUserId ?? string.Empty);
            return View(customerBookings);
        }

        // ==========================================
        // GET: Booking/Requests (Vehicle Owner Only)
        // Dedicated request management inbox for fleet owners
        // ==========================================
        [Authorize(Roles = "VehicleOwner,Admin")]
        public async Task<IActionResult> Requests()
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            IEnumerable<Booking> requests;

            if (User.IsInRole("Admin"))
            {
                requests = await _bookingService.GetAllAsync();
            }
            else
            {
                requests = await _bookingService.GetByOwnerIdAsync(currentUserId ?? string.Empty);
            }

            return View(requests);
        }

        // ==========================================
        // POST: Booking/Accept (Vehicle Owner Only)
        // Owner accepts a customer's rental request (Pending -> Accepted)
        // ==========================================
        [HttpPost]
        [Authorize(Roles = "VehicleOwner,Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Accept(int id)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var result = await _bookingService.AcceptBookingAsync(id, currentUserId ?? string.Empty, User.IsInRole("Admin"));

            if (result.Success)
            {
                TempData["Success"] = result.Message;
            }
            else
            {
                TempData["Error"] = result.Message;
            }

            return RedirectToAction(nameof(Requests));
        }

        // ==========================================
        // POST: Booking/Reject (Vehicle Owner Only)
        // Owner rejects a customer's rental request (Pending -> Rejected)
        // ==========================================
        [HttpPost]
        [Authorize(Roles = "VehicleOwner,Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(int id)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var result = await _bookingService.RejectBookingAsync(id, currentUserId ?? string.Empty, User.IsInRole("Admin"));

            if (result.Success)
            {
                TempData["Success"] = result.Message;
            }
            else
            {
                TempData["Error"] = result.Message;
            }

            return RedirectToAction(nameof(Requests));
        }

        // ==========================================
        // GET: Booking/Details/5
        // ==========================================
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var booking = await _bookingService.GetByIdAsync(id.Value);
            if (booking == null)
            {
                return NotFound();
            }

            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Authorization check: Customer can only view their own; Owner can only view bookings on their vehicles; Admin sees all
            if (!User.IsInRole("Admin"))
            {
                bool isRenter = booking.UserId == currentUserId;
                bool isVehicleOwner = booking.Vehicle?.OwnerId == currentUserId;

                if (!isRenter && !isVehicleOwner)
                {
                    TempData["Error"] = "Access Denied: You do not have permission to view this reservation.";
                    return Forbid();
                }
            }

            return View(booking);
        }

        // ==========================================
        // GET: Booking/Create (Customer Only)
        // Vehicle Owners are strictly blocked from renting vehicles
        // ==========================================
        public IActionResult Create(int? vehicleId, DateTime? startDate, DateTime? endDate)
        {
            // Business rule: Vehicle Owner cannot rent vehicles
            if (User.IsInRole("VehicleOwner"))
            {
                TempData["Error"] = "Vehicle Owners are not permitted to book or rent vehicles. Please sign in with a Customer account.";
                return RedirectToAction("Index", "Vehicle");
            }

            var start = startDate.HasValue && startDate.Value.Date >= DateTime.Today ? startDate.Value.Date : DateTime.Today;
            var end = endDate.HasValue && endDate.Value.Date > start ? endDate.Value.Date : start.AddDays(3);

            var model = new Booking
            {
                VehicleId = vehicleId ?? 0,
                StartDate = start,
                EndDate = end
            };

            return View(model);
        }

        // ==========================================
        // POST: Booking/Create (Customer Only)
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Booking booking)
        {
            // Backend enforcement: Vehicle Owner cannot rent vehicles
            if (User.IsInRole("VehicleOwner"))
            {
                TempData["Error"] = "Vehicle Owners are not permitted to book or rent vehicles. Please sign in with a Customer account.";
                return RedirectToAction("Index", "Vehicle");
            }

            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Bind to authenticated customer unless admin is booking on behalf
            if (!User.IsInRole("Admin") || string.IsNullOrEmpty(booking.UserId))
            {
                booking.UserId = currentUserId ?? string.Empty;
            }

            // Remove ModelState validation for navigation properties
            ModelState.Remove(nameof(booking.User));
            ModelState.Remove(nameof(booking.Vehicle));
            ModelState.Remove(nameof(booking.Payment));

            if (!ModelState.IsValid)
            {
                return View(booking);
            }

            var result = await _bookingService.CreateBookingAsync(booking);
            if (!result.Success)
            {
                ModelState.AddModelError(string.Empty, result.Message);
                return View(booking);
            }

            TempData["Success"] = "Your booking request has been sent to the vehicle owner. You will be able to make payment once the owner accepts your request.";
            return RedirectToAction(nameof(Index));
        }

        // ==========================================
        // GET: Booking/Edit/5 (Admin Only)
        // ==========================================
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var booking = await _bookingService.GetByIdAsync(id.Value);
            if (booking == null)
            {
                return NotFound();
            }

            return View(booking);
        }

        // ==========================================
        // POST: Booking/Edit/5 (Admin Only)
        // ==========================================
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Booking booking)
        {
            if (id != booking.Id)
            {
                return NotFound();
            }

            ModelState.Remove(nameof(booking.User));
            ModelState.Remove(nameof(booking.Vehicle));
            ModelState.Remove(nameof(booking.Payment));

            if (!ModelState.IsValid)
            {
                return View(booking);
            }

            await _bookingService.UpdateAsync(booking);
            TempData["Success"] = "Booking updated successfully.";

            return RedirectToAction(nameof(Index));
        }

        // ==========================================
        // GET: Booking/Delete/5
        // ==========================================
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var booking = await _bookingService.GetByIdAsync(id.Value);
            if (booking == null)
            {
                return NotFound();
            }

            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!User.IsInRole("Admin"))
            {
                if (booking.UserId != currentUserId)
                {
                    return Forbid();
                }
            }

            return View(booking);
        }

        // ==========================================
        // POST: Booking/Delete/5
        // ==========================================
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var booking = await _bookingService.GetByIdAsync(id);
            if (booking == null)
            {
                return NotFound();
            }

            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!User.IsInRole("Admin") && booking.UserId != currentUserId)
            {
                return Forbid();
            }

            await _bookingService.DeleteAsync(id);
            TempData["Success"] = "Reservation cancelled successfully.";

            return RedirectToAction(nameof(Index));
        }
    }
}