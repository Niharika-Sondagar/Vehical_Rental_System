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

        public async Task<IEnumerable<Vehicle>> GetAllAsync()
        {
            return await _vehicleRepository.GetAllAsync();
        }

        public async Task<Vehicle?> GetByIdAsync(int id)
        {
            return await _vehicleRepository.GetByIdAsync(id);
        }

        public async Task AddAsync(Vehicle vehicle)
        {
            await _vehicleRepository.AddAsync(vehicle);
        }

        public async Task UpdateAsync(Vehicle vehicle)
        {
            await _vehicleRepository.UpdateAsync(vehicle);
        }

        public async Task DeleteAsync(int id)
        {
            await _vehicleRepository.DeleteAsync(id);
        }
    }
}