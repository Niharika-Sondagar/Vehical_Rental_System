using Vehical_Rental.Models;

namespace Vehical_Rental.Services.Interfaces
{
    public interface IBookingService
    {
        Task<IEnumerable<Booking>> GetAllAsync();
        Task<Booking?> GetByIdAsync(int id);

        Task<(bool Success, string Message)> CreateBookingAsync(Booking booking);

        Task UpdateAsync(Booking booking);
        Task DeleteAsync(int id);
    }
}