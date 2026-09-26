using Vehical_Rental.Models;

namespace Vehical_Rental.Repositories.Interfaces
{
    public interface IBookingRepository
    {
        Task<IEnumerable<Booking>> GetAllAsync();
        Task<Booking?> GetByIdAsync(int id);
        Task AddAsync(Booking booking);
        Task<bool> IsVehicleAvailableAsync(int vehicleId,DateTime startDate,DateTime endDate);
        Task UpdateAsync(Booking booking);
        Task DeleteAsync(int id);
    }
}