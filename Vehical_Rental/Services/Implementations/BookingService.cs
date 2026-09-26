using Vehical_Rental.Models;
using Vehical_Rental.Repositories.Interfaces;
using Vehical_Rental.Services.Interfaces;

namespace Vehical_Rental.Services.Implementations
{
    public class BookingService : IBookingService
    {
        private readonly IBookingRepository _bookingRepository;

        public BookingService(IBookingRepository bookingRepository)
        {
            _bookingRepository = bookingRepository;
        }

        public async Task<IEnumerable<Booking>> GetAllAsync()
        {
            return await _bookingRepository.GetAllAsync();
        }

        public async Task<Booking?> GetByIdAsync(int id)
        {
            return await _bookingRepository.GetByIdAsync(id);
        }

        public async Task<(bool Success, string Message)> CreateBookingAsync(Booking booking)
        {
            if (booking.StartDate >= booking.EndDate)
            {
                return (false, "End date must be after start date.");
            }

            var isAvailable = await _bookingRepository.IsVehicleAvailableAsync(
                booking.VehicleId,
                booking.StartDate,
                booking.EndDate);

            if (!isAvailable)
            {
                return (false, "Vehicle is already booked for these dates.");
            }

            await _bookingRepository.AddAsync(booking);

            return (true, "Booking created successfully.");
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