using Vehical_Rental.Models;

namespace Vehical_Rental.Repositories.Interfaces
{
    public interface IVehicleRepository
    {
        Task<IEnumerable<Vehicle>> GetAllAsync();
        Task<Vehicle?> GetByIdAsync(int id);
        Task<IEnumerable<Vehicle>> GetByOwnerIdAsync(string ownerId);
        Task<IEnumerable<Vehicle>> GetNearbyVehiclesAsync(double latitude,double longitude,double radiusKm);
        Task AddAsync(Vehicle vehicle);
        Task UpdateAsync(Vehicle vehicle);
        Task DeleteAsync(int id);
    }
}