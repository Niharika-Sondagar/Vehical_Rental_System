using Vehical_Rental.Models;

namespace Vehical_Rental.Repositories.Interfaces
{
    public interface IReviewRepository
    {
        Task<IEnumerable<Review>> GetAllAsync();

        Task<Review?> GetByIdAsync(int id);

        Task<IEnumerable<Review>> GetByVehicleIdAsync(int vehicleId);

        Task AddAsync(Review review);

        Task UpdateAsync(Review review);

        Task DeleteAsync(int id);
    }
}