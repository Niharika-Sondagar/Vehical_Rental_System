using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Vehical_Rental.Data;
using Vehical_Rental.Models;
using Vehical_Rental.Services.Interfaces;

namespace Vehical_Rental.Controllers
{
    [Authorize]
    public class PaymentController : Controller
    {
        private readonly IBookingService _bookingService;
        private readonly ApplicationDbContext _context;

        public PaymentController(IBookingService bookingService, ApplicationDbContext context)
        {
            _bookingService = bookingService;
            _context = context;
        }

        // ==========================================
        // GET: Payment/Index?bookingId=5
        // Allows customer to pay for an Accepted booking in INR (₹)
        // ==========================================
        [Authorize(Roles = "Customer,Admin")]
        public async Task<IActionResult> Index(int? bookingId)
        {
            if (bookingId == null || bookingId <= 0)
            {
                TempData["Error"] = "Please select an approved booking reservation to proceed with payment.";
                return RedirectToAction("Index", "Booking");
            }

            var booking = await _bookingService.GetByIdAsync(bookingId.Value);
            if (booking == null)
            {
                TempData["Error"] = "Booking reservation could not be found.";
                return RedirectToAction("Index", "Booking");
            }

            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Authorization: Only the booking customer or Admin can pay
            if (!User.IsInRole("Admin") && booking.UserId != currentUserId)
            {
                TempData["Error"] = "Access Denied: You cannot make payment on another customer's booking.";
                return Forbid();
            }

            // Workflow enforcement: Payment is ONLY available after owner acceptance
            if (booking.Status == "Pending")
            {
                TempData["Error"] = "Payment is not available yet. Your booking request is still awaiting approval from the vehicle owner.";
                return RedirectToAction("Details", "Booking", new { id = booking.Id });
            }

            if (booking.Status == "Rejected")
            {
                TempData["Error"] = "This booking request was declined by the vehicle owner. No payment is required.";
                return RedirectToAction("Details", "Booking", new { id = booking.Id });
            }

            if (booking.Status == "Confirmed" || booking.Status == "Completed")
            {
                TempData["Info"] = "This reservation has already been confirmed and paid.";
                return RedirectToAction("Details", "Booking", new { id = booking.Id });
            }

            if (booking.Status == "Cancelled")
            {
                TempData["Error"] = "This booking has been cancelled.";
                return RedirectToAction("Index", "Booking");
            }

            return View(booking);
        }

        // ==========================================
        // POST: Payment/Process
        // Processes payment in INR and confirms the booking
        // ==========================================
        [HttpPost]
        [Authorize(Roles = "Customer,Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Process(int bookingId, string paymentMethod)
        {
            var booking = await _bookingService.GetByIdAsync(bookingId);
            if (booking == null)
            {
                TempData["Error"] = "Booking reservation could not be found.";
                return RedirectToAction("Index", "Booking");
            }

            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!User.IsInRole("Admin") && booking.UserId != currentUserId)
            {
                TempData["Error"] = "Access Denied: You cannot make payment on another customer's booking.";
                return Forbid();
            }

            if (booking.Status != "Accepted")
            {
                TempData["Error"] = $"Cannot process payment: booking status is '{booking.Status}'. Payment is only allowed for approved bookings.";
                return RedirectToAction("Details", "Booking", new { id = booking.Id });
            }

            // Normalize payment method
            var method = string.IsNullOrWhiteSpace(paymentMethod) ? "UPI" : paymentMethod.Trim();

            // Create verified Payment record in Indian Rupees (INR)
            var payment = new Payment
            {
                BookingId = booking.Id,
                Amount = booking.TotalPrice,
                PaymentDate = DateTime.UtcNow,
                PaymentMethod = method,
                PaymentStatus = "Completed",
                TransactionId = "TXN" + DateTime.UtcNow.ToString("yyyyMMdd") + Guid.NewGuid().ToString("N")[..8].ToUpper()
            };

            await _context.Payments.AddAsync(payment);

            // Update booking status from Accepted -> Confirmed
            booking.Status = "Confirmed";
            await _bookingService.UpdateAsync(booking);

            TempData["Success"] = $"Payment of ₹{booking.TotalPrice:N2} via {method} was completed successfully! Your booking is confirmed. Transaction ID: {payment.TransactionId}.";
            return RedirectToAction("Details", "Booking", new { id = booking.Id });
        }

        // ==========================================
        // GET: Payment/PaymentManagement (Admin Only)
        // ==========================================
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> PaymentManagement()
        {
            var payments = await _context.Payments
                .Include(p => p.Booking)
                    .ThenInclude(b => b!.User)
                .Include(p => p.Booking)
                    .ThenInclude(b => b!.Vehicle)
                .OrderByDescending(p => p.PaymentDate)
                .ToListAsync();

            return View(payments);
        }
    }
}
