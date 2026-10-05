using Vehical_Rental.Models;

namespace Vehical_Rental.Repositories.Interfaces
{
    public interface IBookingRepository
    {
        Task<IEnumerable<Booking>> GetAllAsync();
        Task<Booking?> GetByIdAsync(int id);
        Task<IEnumerable<Booking>> GetByOwnerIdAsync(string ownerId);
        Task<IEnumerable<Booking>> GetByCustomerIdAsync(string customerId);
        Task AddAsync(Booking booking);
        Task<bool> IsVehicleAvailableAsync(int vehicleId, DateTime startDate, DateTime endDate, int? excludeBookingId = null);
        Task<bool> HasCustomerActiveBookingOverlapAsync(string customerId, DateTime startDate, DateTime endDate, int? excludeBookingId = null);
        Task UpdateAsync(Booking booking);
        Task DeleteAsync(int id);
    }
}