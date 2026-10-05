using Vehical_Rental.Models;
using Vehical_Rental.Repositories.Interfaces;
using Vehical_Rental.Services.Interfaces;

namespace Vehical_Rental.Services.Implementations
{
    public class BookingService : IBookingService
    {
        private readonly IBookingRepository _bookingRepository;
        private readonly IVehicleRepository _vehicleRepository;

        public BookingService(IBookingRepository bookingRepository, IVehicleRepository vehicleRepository)
        {
            _bookingRepository = bookingRepository;
            _vehicleRepository = vehicleRepository;
        }

        public async Task<IEnumerable<Booking>> GetAllAsync()
        {
            return await _bookingRepository.GetAllAsync();
        }

        public async Task<Booking?> GetByIdAsync(int id)
        {
            return await _bookingRepository.GetByIdAsync(id);
        }

        public async Task<IEnumerable<Booking>> GetByOwnerIdAsync(string ownerId)
        {
            return await _bookingRepository.GetByOwnerIdAsync(ownerId);
        }

        public async Task<IEnumerable<Booking>> GetByCustomerIdAsync(string customerId)
        {
            return await _bookingRepository.GetByCustomerIdAsync(customerId);
        }

        public async Task<(bool Success, string Message)> CreateBookingAsync(Booking booking)
        {
            booking.StartDate = booking.StartDate.Date;
            booking.EndDate = booking.EndDate.Date;

            if (booking.StartDate < DateTime.Today)
            {
                return (false, "Pickup date cannot be in the past.");
            }

            if (booking.StartDate >= booking.EndDate)
            {
                return (false, "Return date must be after pickup date.");
            }

            var vehicle = await _vehicleRepository.GetByIdAsync(booking.VehicleId);
            if (vehicle == null)
            {
                return (false, "Selected vehicle could not be found.");
            }

            if (!vehicle.IsAvailable)
            {
                return (false, "Vehicle is currently marked as unavailable for rent.");
            }

            // Check if user is booking their own vehicle
            if (!string.IsNullOrEmpty(vehicle.OwnerId) && vehicle.OwnerId == booking.UserId)
            {
                return (false, "Vehicle Owners cannot book their own vehicles.");
            }

            // 1. Check if the customer already has an active booking for overlapping dates
            var hasCustomerConflict = await _bookingRepository.HasCustomerActiveBookingOverlapAsync(
                booking.UserId,
                booking.StartDate,
                booking.EndDate);

            if (hasCustomerConflict)
            {
                return (false, "You already have an active rental booking for the selected dates. A customer cannot rent vehicles on the same date multiple times.");
            }

            // 2. Check if the vehicle is available for the selected dates
            var isAvailable = await _bookingRepository.IsVehicleAvailableAsync(
                booking.VehicleId,
                booking.StartDate,
                booking.EndDate);

            if (!isAvailable)
            {
                return (false, "This vehicle is not available for the selected dates.");
            }

            // Calculate reliable total price from vehicle rate
            var totalDays = Math.Max(1, (booking.EndDate - booking.StartDate).Days);
            booking.TotalPrice = totalDays * vehicle.DailyRate;
            booking.Status = "Pending";
            booking.BookingDate = DateTime.UtcNow;

            await _bookingRepository.AddAsync(booking);

            return (true, "Your booking request has been sent to the vehicle owner.");
        }

        public async Task<(bool Success, string Message)> AcceptBookingAsync(int bookingId, string ownerId, bool isAdmin = false)
        {
            var booking = await _bookingRepository.GetByIdAsync(bookingId);
            if (booking == null)
            {
                return (false, "Booking request not found.");
            }

            // Verify vehicle ownership or admin rights
            if (!isAdmin && booking.Vehicle?.OwnerId != ownerId)
            {
                return (false, "Access Denied: You can only review booking requests for vehicles in your fleet.");
            }

            if (booking.Status != "Pending")
            {
                return (false, $"Only pending requests can be accepted. Current status is '{booking.Status}'.");
            }

            // Re-verify availability to prevent double-booking race conditions
            var isAvailable = await _bookingRepository.IsVehicleAvailableAsync(
                booking.VehicleId,
                booking.StartDate,
                booking.EndDate,
                excludeBookingId: booking.Id);

            if (!isAvailable)
            {
                return (false, "Cannot accept: This vehicle is not available for the selected dates due to a conflicting booking.");
            }

            // Re-verify that customer doesn't have an overlapping confirmed/accepted booking
            var hasCustomerConflict = await _bookingRepository.HasCustomerActiveBookingOverlapAsync(
                booking.UserId,
                booking.StartDate,
                booking.EndDate,
                excludeBookingId: booking.Id);

            if (hasCustomerConflict)
            {
                return (false, "Cannot accept: The customer already has another active booking for these overlapping dates.");
            }

            booking.Status = "Accepted";
            await _bookingRepository.UpdateAsync(booking);

            return (true, $"Booking request #BK-{booking.Id} has been accepted. The customer can now proceed with payment.");
        }

        public async Task<(bool Success, string Message)> RejectBookingAsync(int bookingId, string ownerId, bool isAdmin = false)
        {
            var booking = await _bookingRepository.GetByIdAsync(bookingId);
            if (booking == null)
            {
                return (false, "Booking request not found.");
            }

            // Verify vehicle ownership or admin rights
            if (!isAdmin && booking.Vehicle?.OwnerId != ownerId)
            {
                return (false, "Access Denied: You can only review booking requests for vehicles in your fleet.");
            }

            if (booking.Status == "Confirmed" || booking.Status == "Completed")
            {
                return (false, $"Cannot reject an already confirmed or completed booking.");
            }

            booking.Status = "Rejected";
            await _bookingRepository.UpdateAsync(booking);

            return (true, $"Booking request #BK-{booking.Id} has been rejected.");
        }

        public async Task UpdateAsync(Booking booking)
        {
            await _bookingRepository.UpdateAsync(booking);
        }

        public async Task DeleteAsync(int id)
        {
            await _bookingRepository.DeleteAsync(id);
        }
    }
}