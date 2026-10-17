using Vehical_Rental.Models;
using Vehical_Rental.Repositories.Interfaces;
using Vehical_Rental.Services.Interfaces;

namespace Vehical_Rental.Services.Implementations
{
    public class VehicleService : IVehicleService
    {
        private readonly IVehicleRepository _vehicleRepository;

        public VehicleService(IVehicleRepository vehicleRepository)
        {
            _vehicleRepository = vehicleRepository;
        }

        // ==========================================
        // GET ALL VEHICLES
        // ==========================================
        public async Task<IEnumerable<Vehicle>> GetAllAsync()
        {
            return await _vehicleRepository.GetAllAsync();
        }

        // ==========================================
        // GET VEHICLE BY ID
        // ==========================================
        public async Task<Vehicle?> GetByIdAsync(int id)
        {
            return await _vehicleRepository.GetByIdAsync(id);
        }

        // ==========================================
        // GET VEHICLES BY OWNER
        // ==========================================
        public async Task<IEnumerable<Vehicle>> GetByOwnerIdAsync(string ownerId)
        {
            return await _vehicleRepository.GetByOwnerIdAsync(ownerId);
        }

        // ==========================================
        // GET NEARBY VEHICLES
        // ==========================================
        public async Task<IEnumerable<Vehicle>> GetNearbyVehiclesAsync(
            double latitude,
            double longitude,
            double radiusKm)
        {
            return await _vehicleRepository.GetNearbyVehiclesAsync(
                latitude,
                longitude,
                radiusKm);
        }

        // ==========================================
        // ADD VEHICLE
        // ==========================================
        public async Task AddAsync(Vehicle vehicle)
        {
            await _vehicleRepository.AddAsync(vehicle);
        }

        // ==========================================
        // UPDATE VEHICLE
        // ==========================================
        public async Task UpdateAsync(Vehicle vehicle)
        {
            await _vehicleRepository.UpdateAsync(vehicle);
        }

        // ==========================================
        // DELETE VEHICLE
        // ==========================================
        public async Task DeleteAsync(int id)
        {
            await _vehicleRepository.DeleteAsync(id);
        }
    }
}