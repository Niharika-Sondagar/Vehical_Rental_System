using Vehical_Rental.Models;

namespace Vehical_Rental.Services.Interfaces
{
    public interface IBookingService
    {
        Task<IEnumerable<Booking>> GetAllAsync();
        Task<Booking?> GetByIdAsync(int id);
        Task<IEnumerable<Booking>> GetByOwnerIdAsync(string ownerId);
        Task<IEnumerable<Booking>> GetByCustomerIdAsync(string customerId);
        Task<(bool Success, string Message)> CreateBookingAsync(Booking booking);
        Task<(bool Success, string Message)> AcceptBookingAsync(int bookingId, string ownerId, bool isAdmin = false);
        Task<(bool Success, string Message)> RejectBookingAsync(int bookingId, string ownerId, bool isAdmin = false);
        Task UpdateAsync(Booking booking);
        Task DeleteAsync(int id);
    }
}