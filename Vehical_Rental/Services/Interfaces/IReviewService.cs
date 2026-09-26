using Vehical_Rental.Models;

namespace Vehical_Rental.Services.Interfaces
{
    public interface IReviewService
    {
        Task<IEnumerable<Review>> GetAllAsync();

        Task<Review?> GetByIdAsync(int id);

        Task<IEnumerable<Review>> GetByVehicleIdAsync(int vehicleId);

        Task<(bool Success, string Message)> AddAsync(Review review);

        Task UpdateAsync(Review review);

        Task DeleteAsync(int id);
    }
}